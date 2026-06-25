using Application.EWS.Interfaces;
using AutoMapper;
using Domain.EWS.DataModels.Request.Attendance;
using Domain.EWS.DataModels.Response.Attendance;
using Domain.EWS.Interface;
using Shared.EWS.Entities;
using Shared.EWS.Enums;
using Shared.EWS.Exceptions;
using Shared.EWS.Services;
using System.Globalization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Application.EWS.Services
{
    public class AttendanceService(
        IAttendanceRepository repository,
        IPublicHolidayRepository publicHolidayRepository,
        IMapper mapper,
        ClaimsPrincipal principal)
        : GenericService<Attendance>(repository, principal), IAttendanceService
    {
        private readonly IAttendanceRepository _attendanceRepository = repository;
        private readonly IPublicHolidayRepository _publicHolidayRepository = publicHolidayRepository;
        private readonly IMapper _mapper = mapper;

        private bool IsAdmin => CurrentRoleId == 1;
        private bool IsTeamLead => CurrentRoleId == 2;
        private bool IsEmployee => CurrentRoleId == 3;

        public async Task<AttendanceResponse> GetByIdAsync(int id)
        {
            var attendance = await _attendanceRepository.GetWithDetailsAsync(id)
                ?? throw new NotFoundException($"Attendance record with id '{id}' was not found.");

            await AuthorizeViewAsync(attendance);
            return _mapper.Map<AttendanceResponse>(attendance);
        }

        public async Task<AttendanceMonthResponse> GetMonthlyAsync(AttendanceMonthRequest request)
        {
            ValidateMonthYear(request.Month, request.Year);

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

            var joinDate = await _attendanceRepository.GetUserJoinDateAsync(targetUserId)
                           ?? DateTime.SpecifyKind(new DateTime(request.Year, request.Month, 1), DateTimeKind.Utc);

            var records = await _attendanceRepository.GetMonthlyAsync(targetUserId, request.Month, request.Year);

            var holidays = await _publicHolidayRepository.GetHolidaysForMonthAsync(request.Month, request.Year);
            var holidayMap = holidays.ToDictionary(h => h.HolidayDate.Day, h => h.Name);

            string userName = records.FirstOrDefault()?.User?.Name ?? string.Empty;
            bool isViewingOwnCalendar = targetUserId == CurrentUserId;

            return BuildMonthResponse(
                targetUserId, userName, records, joinDate, holidayMap,
                request.Month, request.Year, isViewingOwnCalendar);
        }

        public async Task<AttendanceTeamMonthResponse> GetTeamMonthlyAsync(AttendanceTeamMonthRequest request)
        {
            ValidateMonthYear(request.Month, request.Year);

            if (IsEmployee)
                throw new ForbiddenException("Employees cannot view team attendance.");

            var userIds = request.UserIds.Distinct().ToList();
            if (userIds.Count == 0)
                return new AttendanceTeamMonthResponse
                {
                    Month = request.Month,
                    Year = request.Year,
                    MonthLabel = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(request.Month)}-{request.Year}",
                    Members = new()
                };

            if (IsTeamLead)
            {
                var memberIds = await _attendanceRepository.GetTeamMemberIdsAsync(CurrentUserId);
                if (userIds.Except(memberIds).Any())
                    throw new ForbiddenException("You can only view attendance of your own team members.");
            }

            var joinDates = await _attendanceRepository.GetUserJoinDatesAsync(userIds);
            var records = await _attendanceRepository.GetMonthlyBatchAsync(userIds, request.Month, request.Year);
            var recordsByUser = records.GroupBy(r => r.UserId).ToDictionary(g => g.Key, g => g.ToList());

            var holidays = await _publicHolidayRepository.GetHolidaysForMonthAsync(request.Month, request.Year);
            var holidayMap = holidays.ToDictionary(h => h.HolidayDate.Day, h => h.Name);

            var members = new List<AttendanceMonthResponse>(userIds.Count);

            foreach (var userId in userIds)
            {
                var userRecords = recordsByUser.TryGetValue(userId, out var r) ? r : new List<Attendance>();
                var joinDate = joinDates.TryGetValue(userId, out var jd)
                    ? jd
                    : DateTime.SpecifyKind(new DateTime(request.Year, request.Month, 1), DateTimeKind.Utc);
                string userName = userRecords.FirstOrDefault()?.User?.Name ?? string.Empty;

                members.Add(BuildMonthResponse(
                    userId, userName, userRecords, joinDate, holidayMap,
                    request.Month, request.Year, isViewingOwnCalendar: false));
            }

            return new AttendanceTeamMonthResponse
            {
                Month = request.Month,
                Year = request.Year,
                MonthLabel = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(request.Month)}-{request.Year}",
                Members = members
            };
        }

        private static void ValidateMonthYear(int month, int year)
        {
            if (month < 1 || month > 12)
                throw new ArgumentException("Month must be between 1 and 12.");
            if (year < 2000 || year > 2100)
                throw new ArgumentException("Invalid year.");
        }

        private static string DowAbbr(DayOfWeek d) => d switch
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

        private static AttendanceMonthResponse BuildMonthResponse(
            int targetUserId,
            string userName,
            List<Attendance> records,
            DateTime joinDate,
            Dictionary<int, string> holidayMap,
            int month,
            int year,
            bool isViewingOwnCalendar)
        {
            var recordMap = records.ToDictionary(r => r.AttendanceDate.Day);

            var today = DateTime.UtcNow.Date;
            int daysInMonth = DateTime.DaysInMonth(year, month);

            var days = new List<AttendanceDayResponse>(daysInMonth);
            double presentCount = 0;
            double absentCount = 0;
            int workingDays = 0;

            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateTime(year, month, d, 0, 0, 0, DateTimeKind.Utc);
                var dow = date.DayOfWeek;
                bool isWeekend = dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday;
                bool isPublicHoliday = holidayMap.TryGetValue(d, out var holidayName);
                bool isBeforeJoining = date.Date < joinDate.Date;

                if(!isBeforeJoining && !isWeekend && !isPublicHoliday)
                {
                    workingDays++;
                }

                recordMap.TryGetValue(d, out var rec);

                if (!isBeforeJoining)
                {
                    if (rec != null)
                    {
                        switch (rec.Status)
                        {
                            case AttendanceStatus.Present_WFO:
                                presentCount += 1;
                                break;
                            case AttendanceStatus.Present_WFH:
                                presentCount += 1;
                                break;

                            case AttendanceStatus.Absent:
                                absentCount += 1;
                                break;

                            case AttendanceStatus.HalfDay_WFH:
                                presentCount += 0.5;
                                absentCount += 0.5;
                                break;
                            case AttendanceStatus.HalfDay_WFO:
                                presentCount += 0.5;
                                absentCount += 0.5;
                                break;
                        }
                    }
                    else if (!isWeekend && !isPublicHoliday && date.Date < today)
                    {
                        absentCount++;
                    }
                }

                bool isToday = date.Date == today;
                bool canEdit;

                if (isWeekend || isPublicHoliday)
                {
                    canEdit = false;
                }
                else if (isViewingOwnCalendar)
                {
                    canEdit = isToday
                              && rec?.ApprovalStatus != ApprovalStatus.Approved;
                }
                else
                {
                    canEdit = date.Date <= today;
                }

                days.Add(new AttendanceDayResponse
                {
                    Day = d,
                    DayName = DowAbbr(dow),
                    IsWeekend = isWeekend,
                    IsToday = isToday,
                    AttendanceId = rec?.Id,
                    Status = rec?.Status,
                    ApprovalStatus = rec?.ApprovalStatus,
                    IsAutoAbsent = !isBeforeJoining && rec == null && !isWeekend && !isPublicHoliday && date.Date < today,
                    CanEdit = canEdit,
                    IsPublicHoliday = isPublicHoliday,
                    HolidayName = isPublicHoliday ? holidayName : null,
                });
            }

            return new AttendanceMonthResponse
            {
                Month = month,
                Year = year,
                MonthLabel = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month)}-{year}",
                UserId = targetUserId,
                UserName = userName,
                TotalDays = workingDays,
                PresentCount = presentCount,
                AbsentCount = absentCount,
                Days = days
            };
        }

        public async Task<AttendanceResponse> AddAsync(AddAttendanceRequest request)
        {
            var today = DateTime.UtcNow.Date;

            if (await _attendanceRepository.IsPublicHolidayAsync(today))
                throw new InvalidOperationException("Today is a public holiday. Attendance cannot be submitted.");

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

        public async Task<AttendanceResponse> AdminAddAsync(AdminAddAttendanceRequest request)
        {
            if (!IsAdmin && !IsTeamLead)
                throw new ForbiddenException("Only Admins or Team Leads can fill attendance on behalf of others.");

            if (IsTeamLead)
            {
                if (request.UserId == CurrentUserId)
                    throw new ForbiddenException("Team Leads cannot fill their own attendance.");

                var memberIds = await _attendanceRepository.GetTeamMemberIdsAsync(CurrentUserId);
                if (!memberIds.Contains(request.UserId))
                    throw new ForbiddenException("You can fill attendance of your team-member only.");
            }

            var targetDate = DateTime.SpecifyKind(
                request.AttendanceDate?.Date ?? DateTime.UtcNow.Date,
                DateTimeKind.Utc);

            if (targetDate >= DateTime.UtcNow.Date)
                throw new InvalidOperationException(
                    "Only past attendance can be filled; user must manage their own attendance for today.");

            if (await _attendanceRepository.IsPublicHolidayAsync(targetDate))
                throw new InvalidOperationException(
                    $"Attendance cannot be filled for {targetDate:yyyy-MM-dd} as it is a public holiday.");

            var duplicate = await _attendanceRepository.ExistsForDateAsync(request.UserId, targetDate);
            if (duplicate)
                throw new DuplicateRecordException(
                    $"Attendance for this user has already been submitted for {targetDate:yyyy-MM-dd}.");

            var entity = new Attendance
            {
                UserId = request.UserId,
                AttendanceDate = targetDate,
                Status = request.Status,
                ApprovalStatus = ApprovalStatus.Approved,
                ReviewerId = CurrentUserId,
                ReviewedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(entity);

            var created = await _attendanceRepository.GetWithDetailsAsync(entity.Id)
                ?? throw new InvalidOperationException("Failed to retrieve newly created attendance record.");

            return _mapper.Map<AttendanceResponse>(created);
        }

        public async Task<AttendanceResponse> EditAsync(int id, EditAttendanceRequest request)
        {
            var attendance = await GetAttendanceOrThrowAsync(id);

            if (await _attendanceRepository.IsPublicHolidayAsync(attendance.AttendanceDate))
                throw new InvalidOperationException(
                    $"Attendance on {attendance.AttendanceDate:yyyy-MM-dd} is a public holiday and cannot be edited.");

            if (IsAdmin)
            {
                if (attendance.UserId == CurrentUserId)
                    throw new ForbiddenException("Admins cannot edit their own attendance.");

                if (attendance.AttendanceDate.Date > DateTime.UtcNow.Date)
                    throw new InvalidOperationException(
                        "Future attendance cannot be edited. The user must manage their own attendance for today.");
            }
            else if (IsTeamLead)
            {
                if (attendance.UserId == CurrentUserId)
                {
                    if (attendance.ApprovalStatus == ApprovalStatus.Approved)
                        throw new InvalidOperationException(
                            "Attendance cannot be edited that has been approved.");
                }
                else
                {
                    var memberIds = await _attendanceRepository.GetTeamMemberIdsAsync(CurrentUserId);
                    if (!memberIds.Contains(attendance.UserId))
                        throw new ForbiddenException("You can only edit attendance for members of your team.");

                    if (attendance.AttendanceDate.Date > DateTime.UtcNow.Date)
                        throw new InvalidOperationException(
                            "Future attendance cannot be edited. The employee must manage their own attendance for today.");
                }
            }
            else
            {
                if (attendance.UserId != CurrentUserId)
                    throw new ForbiddenException("You can only edit your own attendance.");

                if (attendance.ApprovalStatus == ApprovalStatus.Approved)
                    throw new InvalidOperationException(
                        "Attendance cannot be edited that has been approved.");
            }

            attendance.Status = request.Status;
            attendance.UpdatedAt = DateTime.UtcNow;

            if ((IsAdmin || IsTeamLead) && attendance.UserId != CurrentUserId)
            {
                attendance.ApprovalStatus = ApprovalStatus.Approved;
                attendance.ReviewerId = CurrentUserId;
                attendance.ReviewedAt = DateTime.UtcNow;
            }
            else
            {
                if (attendance.ApprovalStatus == ApprovalStatus.Rejected)
                {
                    attendance.ApprovalStatus = ApprovalStatus.Pending;
                    attendance.ReviewerId = null;
                    attendance.ReviewerRemark = null;
                    attendance.ReviewedAt = null;
                }
            }

            await _repository.UpdateAsync(attendance);

            var updated = await _attendanceRepository.GetWithDetailsAsync(id)
                ?? throw new InvalidOperationException("Failed to retrieve updated attendance record.");

            return _mapper.Map<AttendanceResponse>(updated);
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

        public async Task<int> ApproveAllPendingAsync()
        {
            if (IsEmployee)
                throw new ForbiddenException("Employees cannot approve attendance.");

            var pending = await _attendanceRepository.GetAllPendingForReviewAsync(CurrentUserId, IsAdmin);
            if (pending.Count == 0) return 0;

            var now = DateTime.UtcNow;
            foreach (var attendance in pending)
            {
                attendance.ApprovalStatus = ApprovalStatus.Approved;
                attendance.ReviewerId = CurrentUserId;
                attendance.ReviewerRemark = "Noted";
                attendance.ReviewedAt = now;
                attendance.UpdatedAt = now;
            }

            await _repository.UpdateRangeAsync(pending);
            return pending.Count;
        }
    }
}