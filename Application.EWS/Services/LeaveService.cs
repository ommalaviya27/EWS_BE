using Application.EWS.Interfaces;
using AutoMapper;
using Domain.EWS.DataModels.Request.Leave;
using Domain.EWS.DataModels.Response.Leave;
using Domain.EWS.Interface;
using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Enums;
using Shared.EWS.Exceptions;
using Shared.EWS.Services;
using System.Security.Claims;

namespace Application.EWS.Services
{
    public class LeaveService(
        ILeaveRepository repository,
        IAttendanceRepository attendanceRepository,
        IMapper mapper,
        IEmailService emailService,
        ClaimsPrincipal principal)
        : GenericService<LeaveApplication>(repository, principal), ILeaveService
    {
        private readonly ILeaveRepository _leaveRepository = repository;
        private readonly IAttendanceRepository _attendanceRepository = attendanceRepository;
        private readonly IMapper _mapper = mapper;
        private readonly IEmailService _emailService = emailService;

        private bool IsAdmin => CurrentRoleId == 1;
        private bool IsTeamLead => CurrentRoleId == 2;
        private bool IsEmployee => CurrentRoleId == 3;

        public async Task<LeaveResponse> GetByIdAsync(int id)
        {
            var leave = await _leaveRepository.GetWithDetailsAsync(id)
                ?? throw new NotFoundException($"Leave application with id '{id}' was not found.");

            await AuthorizeViewAsync(leave);
            return MapToResponse(leave);
        }

        public async Task<PagedResponse<LeaveResponse>> GetMyLeavesAsync(PaginationRequest pagination)
        {
            var paged = await _leaveRepository.GetMyLeavesAsync(CurrentUserId, pagination);

            return PagedResponse<LeaveResponse>.Create(
                paged.Items.Select(MapToResponse).ToList(),
                paged.TotalCount,
                paged.PageNumber,
                paged.PageSize);
        }

        public async Task<PagedResponse<LeaveResponse>> GetPendingForReviewAsync(PaginationRequest pagination)
        {
            if (IsEmployee)
                throw new ForbiddenException("Employees cannot review leaves.");

            var paged = await _leaveRepository.GetPendingForReviewAsync(CurrentUserId, IsAdmin, pagination);

            return PagedResponse<LeaveResponse>.Create(
                paged.Items.Select(MapToResponse).ToList(),
                paged.TotalCount,
                paged.PageNumber,
                paged.PageSize);
        }

        public async Task<LeaveResponse> ApplyAsync(ApplyLeaveRequest request)
        {
            var startDate = DateTime.SpecifyKind(request.StartDate.Date, DateTimeKind.Utc);
            var endDate = DateTime.SpecifyKind(request.EndDate.Date, DateTimeKind.Utc);

            ValidateDates(startDate, endDate, request.LeaveType);

            if (await _leaveRepository.HasOverlapAsync(CurrentUserId, startDate, endDate))
                throw new DuplicateRecordException("You already have a leave application that overlaps with the requested dates.");

            var entity = new LeaveApplication
            {
                UserId = CurrentUserId,
                LeaveType = request.LeaveType,
                StartDate = startDate,
                EndDate = endDate,
                Reason = request.Reason.Trim(),
                LeaveStatus = ApprovalStatus.Pending
            };

            await _repository.AddAsync(entity);

            var created = await _leaveRepository.GetWithDetailsAsync(entity.Id)
                ?? throw new InvalidOperationException("Failed to retrieve newly created leave application.");

            return MapToResponse(created);
        }

        public async Task<LeaveResponse> EditAsync(int id, EditLeaveRequest request)
        {
            var leave = await _leaveRepository.GetWithDetailsAsync(id)
                ?? throw new NotFoundException($"Leave application with id '{id}' was not found.");

            if (leave.UserId != CurrentUserId)
                throw new ForbiddenException("You can only edit your own leave applications.");

            if (leave.LeaveStatus != ApprovalStatus.Pending)
                throw new InvalidOperationException("Only pending leave applications can be edited.");

            var startDate = DateTime.SpecifyKind(request.StartDate.Date, DateTimeKind.Utc);
            var endDate = DateTime.SpecifyKind(request.EndDate.Date, DateTimeKind.Utc);

            ValidateDates(startDate, endDate, request.LeaveType);

            if (await _leaveRepository.HasOverlapAsync(CurrentUserId, startDate, endDate, excludeId: id))
                throw new DuplicateRecordException("The updated dates overlap with another leave application.");

            var tracked = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException($"Leave application with id '{id}' was not found.");

            tracked.LeaveType = request.LeaveType;
            tracked.StartDate = startDate;
            tracked.EndDate = endDate;
            tracked.Reason = request.Reason.Trim();

            await _repository.UpdateAsync(tracked);

            var updated = await _leaveRepository.GetWithDetailsAsync(id)
                ?? throw new InvalidOperationException("Failed to retrieve updated leave application.");

            return MapToResponse(updated);
        }

        public async Task<LeaveResponse> ReviewAsync(int id, ReviewLeaveRequest request)
        {
            if (IsEmployee)
                throw new ForbiddenException("Employees cannot review leave applications.");

            if (request.LeaveStatus == ApprovalStatus.Pending)
                throw new InvalidOperationException("Review decision must be Approved or Rejected.");

            var leave = await _leaveRepository.GetWithDetailsAsync(id)
                ?? throw new NotFoundException($"Leave application with id '{id}' was not found.");

            if (leave.LeaveStatus != ApprovalStatus.Pending)
                throw new InvalidOperationException("This leave application has already been reviewed and is locked.");

            if (IsTeamLead)
            {
                var memberIds = await _leaveRepository.GetTeamMemberIdsAsync(CurrentUserId);
                if (!memberIds.Contains(leave.UserId))
                    throw new ForbiddenException("You can only review leave applications of your own team members.");
            }
            else
            {
                if (leave.User == null || leave.User.RoleId != 2)
                    throw new ForbiddenException("Admins can only review leave applications of Team Leads.");
            }

            var tracked = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException($"Leave application with id '{id}' was not found.");

            tracked.LeaveStatus = request.LeaveStatus;
            tracked.ReviewerId = CurrentUserId;
            tracked.ReviewerRemark = request.ReviewerRemark;
            tracked.ReviewedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(tracked);

            if (request.LeaveStatus == ApprovalStatus.Approved)
                await PlaceAbsentAttendanceAsync(leave);

            _ = Task.Run(async () =>
            {
                try
                {
                    if (leave.User == null) return;

                    if (request.LeaveStatus == ApprovalStatus.Approved)
                        await _emailService.SendLeaveApprovedEmailAsync(
                            leave.User.Email, leave.User.Name,
                            leave.StartDate, leave.EndDate,
                            leave.LeaveType.ToString(), request.ReviewerRemark);
                    else
                        await _emailService.SendLeaveRejectedEmailAsync(
                            leave.User.Email, leave.User.Name,
                            leave.StartDate, leave.EndDate,
                            leave.LeaveType.ToString(), request.ReviewerRemark);
                }
                catch { /* swallow — email must not fail the response */ }
            });

            var reviewed = await _leaveRepository.GetWithDetailsAsync(id)
                ?? throw new InvalidOperationException("Failed to retrieve reviewed leave application.");

            return MapToResponse(reviewed);
        }

        private static void ValidateDates(DateTime startDate, DateTime endDate, LeaveType leaveType)
        {
            if (startDate < DateTime.UtcNow.Date)
                throw new InvalidOperationException("Leave start date cannot be in the past.");

            if (endDate < startDate)
                throw new InvalidOperationException("End date cannot be before start date.");

            if (leaveType == LeaveType.HalfDay && startDate != endDate)
                throw new InvalidOperationException("Half-day leave must have the same start and end date.");

            var hasWeekday = false;
            for (var d = startDate; d <= endDate; d = d.AddDays(1))
            {
                if (d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                {
                    hasWeekday = true;
                    break;
                }
            }

            if (!hasWeekday)
                throw new InvalidOperationException(
                    "Leave cannot be applied on weekends. Please select at least one working day.");
        }

        private async Task PlaceAbsentAttendanceAsync(LeaveApplication leave)
        {
            for (var d = leave.StartDate.Date; d <= leave.EndDate.Date; d = d.AddDays(1))
            {
                var utcDate = DateTime.SpecifyKind(d, DateTimeKind.Utc);
                if (utcDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    continue;

                if (await _attendanceRepository.ExistsForDateAsync(leave.UserId, utcDate))
                    continue;

                await _attendanceRepository.AddAsync(new Attendance
                {
                    UserId = leave.UserId,
                    AttendanceDate = utcDate,
                    Status = leave.LeaveType == LeaveType.HalfDay
                        ? AttendanceStatus.HalfDay_WFO
                        : AttendanceStatus.Absent,
                    ApprovalStatus = ApprovalStatus.Approved,
                    ReviewerId = CurrentUserId,
                    ReviewedAt = DateTime.UtcNow,
                    ReviewerRemark = $"Auto-placed: Leave approved (Id: {leave.Id})"
                });
            }
        }

        private async Task AuthorizeViewAsync(LeaveApplication leave)
        {
            if (IsAdmin || leave.UserId == CurrentUserId) return;

            if (IsTeamLead)
            {
                var memberIds = await _leaveRepository.GetTeamMemberIdsAsync(CurrentUserId);
                if (memberIds.Contains(leave.UserId)) return;
            }

            throw new ForbiddenException("You do not have access to this leave application.");
        }

        private LeaveResponse MapToResponse(LeaveApplication leave)
        {
            var response = _mapper.Map<LeaveResponse>(leave);
            response.CanEdit = leave.UserId == CurrentUserId && leave.LeaveStatus == ApprovalStatus.Pending;
            return response;
        }
    }
}