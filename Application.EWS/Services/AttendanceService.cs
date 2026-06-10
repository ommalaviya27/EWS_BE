using Application.EWS.Interfaces;
using AutoMapper;
using Domain.EWS.DataModels.Request.Attendance;
using Domain.EWS.DataModels.Response.Attendance;
using Domain.EWS.Interface;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Enums;
using Shared.EWS.Exceptions;
using Shared.EWS.Services;
using System.Globalization;
using System.Security.Claims;

namespace Application.EWS.Services
{
    public class AttendanceService(
        IAttendanceRepository repository,
        IMapper mapper,
        ClaimsPrincipal principal)
        : GenericService<Attendance>(repository, principal), IAttendanceService
    {
        private readonly IAttendanceRepository _attendanceRepository = repository;
        private readonly IMapper _mapper = mapper;

        private bool IsAdmin => CurrentRoleId == 1;
        private bool IsTeamLead => CurrentRoleId == 2;
        private bool IsEmployee => CurrentRoleId == 3;

        public async Task<PagedResponse<AttendanceResponse>> GetAllAsync(AttendanceSearchRequest request)
        {
            int? forceUserId = null;
            List<int>? teamMemberIds = null;

            if (IsEmployee)
            {
                forceUserId = CurrentUserId;
            }
            else if (IsTeamLead)
            {
                var memberIds = await _attendanceRepository.GetTeamMemberIdsAsync(CurrentUserId);
                memberIds.Add(CurrentUserId);
                teamMemberIds = memberIds;
            }

            var paged = await _attendanceRepository.GetAllWithDetailsAsync(request, forceUserId, teamMemberIds);

            return new PagedResponse<AttendanceResponse>
            {
                Items = paged.Items.Select(_mapper.Map<AttendanceResponse>).ToList(),
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize
            };
        }

        public async Task<AttendanceResponse> GetByIdAsync(int id)
        {
            var attendance = await _attendanceRepository.GetWithDetailsAsync(id)
                ?? throw new NotFoundException($"Attendance record with id '{id}' was not found.");

            await AuthorizeViewAsync(attendance);
            return _mapper.Map<AttendanceResponse>(attendance);
        }

        public async Task<AttendanceMonthResponse> GetMonthlyAsync(AttendanceMonthRequest request)
        {
            if (request.Month < 1 || request.Month > 12)
                throw new ArgumentException("Month must be between 1 and 12.");
            if (request.Year < 2000 || request.Year > 2100)
                throw new ArgumentException("Invalid year.");

            int targetUserId;

            if (IsEmployee)
            {
                targetUserId = CurrentUserId;
            }
            else if (IsTeamLead)
            {
                if (request.UserId.HasValue && request.UserId.Value != CurrentUserId)
                {
                    var memberIds = await _attendanceRepository.GetTeamMemberIdsAsync(CurrentUserId);
                    if (!memberIds.Contains(request.UserId.Value))
                        throw new ForbiddenException("You can only view attendance of your own team members.");
                    targetUserId = request.UserId.Value;
                }
                else
                {
                    targetUserId = CurrentUserId;
                }
            }
            else
            {
                targetUserId = request.UserId ?? CurrentUserId;
            }

            var records = await _attendanceRepository.GetMonthlyAsync(targetUserId, request.Month, request.Year);

            var recordMap = records.ToDictionary(r => r.AttendanceDate.Day);

            var today = DateTime.UtcNow.Date;
            int daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);

            static string DowAbbr(DayOfWeek d) => d switch
            {
                DayOfWeek.Monday => "M",
                DayOfWeek.Tuesday => "T",
                DayOfWeek.Wednesday => "W",
                DayOfWeek.Thursday => "T",
                DayOfWeek.Friday => "F",
                DayOfWeek.Saturday => "S",
                DayOfWeek.Sunday => "S",
                _ => "?"
            };

            var days = new List<AttendanceDayResponse>(daysInMonth);
            int presentCount = 0;
            int absentCount = 0;

            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateTime(request.Year, request.Month, d, 0, 0, 0, DateTimeKind.Utc);
                var dow = date.DayOfWeek;
                bool isWeekend = dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday;

                recordMap.TryGetValue(d, out var rec);

                if (rec != null)
                {
                    if (rec.Status == AttendanceStatus.Absent)
                        absentCount++;
                    else
                        presentCount++;
                }
                else if (!isWeekend && date.Date < today)
                {
                    absentCount++;
                }

                days.Add(new AttendanceDayResponse
                {
                    Day = d,
                    DayName = DowAbbr(dow),
                    IsWeekend = isWeekend,
                    IsToday = date.Date == today,
                    AttendanceId = rec?.Id,
                    Status = rec?.Status,
                    ApprovalStatus = rec?.ApprovalStatus,
                    IsAutoAbsent = rec == null && !isWeekend && date.Date < today
                });
            }

            string userName = records.FirstOrDefault()?.User?.Name ?? string.Empty;

            return new AttendanceMonthResponse
            {
                Month = request.Month,
                Year = request.Year,
                MonthLabel = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(request.Month)}-{request.Year}",
                UserId = targetUserId,
                UserName = userName,
                TotalDays = daysInMonth,
                PresentCount = presentCount,
                AbsentCount = absentCount,
                Days = days
            };
        }

        public async Task<AttendanceResponse> AddAsync(AddAttendanceRequest request)
        {
            if (IsAdmin)
                throw new ForbiddenException("Admins cannot submit attendance.");

            var today = DateTime.UtcNow.Date;

            var duplicate = await _attendanceRepository.ExistsForDateAsync(CurrentUserId, today);
            if (duplicate)
                throw new DuplicateRecordException($"Attendance for today ({today:yyyy-MM-dd}) has already been submitted.");

            var entity = new Attendance
            {
                UserId = CurrentUserId,
                AttendanceDate = today,
                Status = request.Status,
                ApprovalStatus = ApprovalStatus.Pending
            };

            await _repository.AddAsync(entity);

            var created = await _attendanceRepository.GetWithDetailsAsync(entity.Id)
                ?? throw new InvalidOperationException("Failed to retrieve newly created attendance record.");

            return _mapper.Map<AttendanceResponse>(created);
        }

        public async Task<AttendanceResponse> EditAsync(int id, EditAttendanceRequest request)
        {
            if (IsAdmin)
                throw new ForbiddenException("Admins do not submit attendance.");

            var attendance = await GetAttendanceOrThrowAsync(id);

            if (attendance.ApprovalStatus != ApprovalStatus.Pending)
                throw new InvalidOperationException(
                    "Attendance cannot be edited that has been reviewed.");

            if (IsEmployee)
            {
                if (attendance.UserId != CurrentUserId)
                    throw new ForbiddenException("You can only edit your own attendance.");
            }
            else if (IsTeamLead)
            {
                if (attendance.UserId != CurrentUserId)
                {
                    var memberIds = await _attendanceRepository.GetTeamMemberIdsAsync(CurrentUserId);
                    if (!memberIds.Contains(attendance.UserId))
                        throw new ForbiddenException("You can only edit attendance for members of your team.");
                }
            }

            attendance.Status = request.Status;
            attendance.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(attendance);

            var updated = await _attendanceRepository.GetWithDetailsAsync(id)
                ?? throw new InvalidOperationException("Failed to retrieve updated attendance record.");

            return _mapper.Map<AttendanceResponse>(updated);
        }

        public async Task<AttendanceResponse> ReviewAsync(int id, ReviewAttendanceRequest request)
        {
            if (IsEmployee)
                throw new ForbiddenException("Employees cannot review attendance.");

            if (request.ApprovalStatus == ApprovalStatus.Pending)
                throw new InvalidOperationException("Review decision must be Approved or Rejected.");

            var attendance = await GetAttendanceOrThrowAsync(id);

            if (attendance.ApprovalStatus != ApprovalStatus.Pending)
                throw new InvalidOperationException(
                    "This attendance record has already been reviewed and is locked.");

            if (IsTeamLead)
            {
                var memberIds = await _attendanceRepository.GetTeamMemberIdsAsync(CurrentUserId);
                if (!memberIds.Contains(attendance.UserId))
                    throw new ForbiddenException("You can only review attendance for members of your team.");
            }

            attendance.ApprovalStatus = request.ApprovalStatus;
            attendance.ReviewerId = CurrentUserId;
            attendance.ReviewerRemark = request.ReviewerRemark;
            attendance.ReviewedAt = DateTime.UtcNow;
            attendance.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(attendance);

            var reviewed = await _attendanceRepository.GetWithDetailsAsync(id)
                ?? throw new InvalidOperationException("Failed to retrieve reviewed attendance record.");

            return _mapper.Map<AttendanceResponse>(reviewed);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (!IsAdmin)
                throw new ForbiddenException("Only Admins can delete attendance records.");

            var attendance = await GetAttendanceOrThrowAsync(id);
            return await _repository.DeleteAsync(attendance.Id);
        }

        private async Task<Attendance> GetAttendanceOrThrowAsync(int id)
        {
            return await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException($"Attendance record with id '{id}' was not found.");
        }

        private async Task AuthorizeViewAsync(Attendance attendance)
        {
            if (IsAdmin) return;

            if (IsTeamLead)
            {
                if (attendance.UserId == CurrentUserId) return;
                var memberIds = await _attendanceRepository.GetTeamMemberIdsAsync(CurrentUserId);
                if (!memberIds.Contains(attendance.UserId))
                    throw new ForbiddenException("You do not have access to this attendance record.");
                return;
            }

            if (attendance.UserId != CurrentUserId)
                throw new ForbiddenException("You do not have access to this attendance record.");
        }
    }
}