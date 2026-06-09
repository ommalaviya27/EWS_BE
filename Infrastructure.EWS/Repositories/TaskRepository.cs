using Domain.EWS.DataModels.Request.Tasks;
using Domain.EWS.DataModels.Response.Project;
using Domain.EWS.DataModels.Response.Tasks;
using Domain.EWS.Interface;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Enums;
using Shared.EWS.Extensions;

namespace Infrastructure.EWS.Repositories
{
    public class TaskRepository(EWSDbContext context)
        : GenericRepository<Tasks>(context), ITaskRepository
    {
        public async Task<PagedResponse<Tasks>> GetAllTasksWithDetailsAsync(
            TaskSearchRequest request,
            Guid? projectId,
            int? assignedToUserId,
            List<Guid>? projectIdFilter)
        {
            var query = _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.AssignedBy)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .Include(t => t.Attachments).ThenInclude(a => a.User)
                .Where(t => !t.IsDeleted)
                .AsQueryable();

            if (projectIdFilter != null)
                query = query.Where(t => projectIdFilter.Contains(t.ProjectId));

            if (assignedToUserId.HasValue)
                query = query.Where(t => t.AssignedToUserId == assignedToUserId.Value);

            if (projectId.HasValue)
                query = query.Where(t => t.ProjectId == projectId.Value);

            var search = request.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(t => EF.Functions.ILike(t.Title, $"%{search}%"));

            if (request.Status.HasValue)
                query = query.Where(t => t.TaskStatus == request.Status.Value);

            if (request.Priority.HasValue)
                query = query.Where(t => t.Priority == request.Priority.Value);

            if (request.DueDateFrom.HasValue)
                query = query.Where(t => t.DueDate >= request.DueDateFrom.Value.ToUniversalTime());

            if (request.DueDateTo.HasValue)
                query = query.Where(t => t.DueDate <= request.DueDateTo.Value.ToUniversalTime().AddDays(1).AddSeconds(-1));

            return await query.ToPagedResponseAsync(request);
        }

        public async Task<Tasks?> GetTaskWithDetailsAsync(int id)
            => await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.AssignedBy)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .Include(t => t.Attachments).ThenInclude(a => a.User)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        public async Task<Projects?> GetProjectByIdAsync(Guid projectId)
            => await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        public async Task<User?> GetAssigneeAsync(int userId)
            => await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        public async Task<List<Guid>> GetTeamLeadProjectIdsAsync(int teamLeadUserId)
            => await _context.Projects
                .Where(p => p.UserId == teamLeadUserId && !p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync();

        public async Task<IEnumerable<User>> GetTeamMembersAsync(int teamLeadUserId)
            => await _context.Users
                .Where(u => u.TeamLeadId == teamLeadUserId && u.RoleId == 3 && !u.IsDeleted)
                .AsNoTracking()
                .ToListAsync();

        public async Task<PagedResponse<GetProjectResponse>> GetProjectsByUserIdAsync(int userId, ProjectListRequest request)
        {
            var query = _context.Projects
                .Where(p => p.UserId == userId && !p.IsDeleted)
                .AsQueryable();

            var search = request.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.Name)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new GetProjectResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    UserId = p.UserId,
                    ProjectStatus = p.ProjectStatus,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    TaskCount = _context.Tasks.Count(t => t.ProjectId == p.Id && !t.IsDeleted),
                })
                .AsNoTracking()
                .ToListAsync();

            return PagedResponse<GetProjectResponse>.Create(items, totalCount, request.PageNumber, request.PageSize);
        }

        public async Task<TeamLeadDashboardResponse> GetTeamTaskCountsAsync(int teamLeadUserId)
        {
            var now = DateTime.UtcNow;

            return await _context.Tasks
                .Where(t => !t.IsDeleted
                         && _context.Projects.Any(p =>
                                p.UserId == teamLeadUserId && !p.IsDeleted && p.Id == t.ProjectId))
                .GroupBy(_ => 1)
                .Select(g => new TeamLeadDashboardResponse
                {
                    MyTeamTaskCount = g.Count(),
                    OverdueTaskCount = g.Count(t => t.DueDate < now && t.TaskStatus != TaskStatuses.Completed),
                })
                .AsNoTracking()
                .FirstOrDefaultAsync()
                ?? new TeamLeadDashboardResponse();
        }

        public async Task<int> GetActiveProjectCountAsync(int teamLeadUserId)
            => await _context.Projects
                .CountAsync(p => p.UserId == teamLeadUserId
                              && !p.IsDeleted
                              && p.ProjectStatus == ProjectStatus.Active);

        public async Task<List<GetTaskResponse>> GetRecentTeamTasksAsync(int teamLeadUserId, int take = 5)
        {
            return await _context.Tasks
                .Where(t => !t.IsDeleted
                         && t.TaskStatus == TaskStatuses.Completed
                         && _context.Projects.Any(p =>
                                p.UserId == teamLeadUserId && !p.IsDeleted && p.Id == t.ProjectId))
                .OrderByDescending(t => t.UpdatedAt)
                .Take(take)
                .Select(t => new GetTaskResponse
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project != null ? t.Project.Name : string.Empty,
                    AssignedToUserId = t.AssignedToUserId,
                    AssignedToUserName = t.AssignedTo != null ? t.AssignedTo.Name : string.Empty,
                    AssignedByUserId = t.AssignedByUserId,
                    AssignedByUserName = t.AssignedBy != null ? t.AssignedBy.Name : string.Empty,
                    TaskStatus = t.TaskStatus,
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<GetTaskResponse>> GetOverdueTeamTasksAsync(int teamLeadUserId, int take = 5)
        {
            var now = DateTime.UtcNow;

            return await _context.Tasks
                .Where(t => !t.IsDeleted
                         && t.DueDate < now
                         && t.TaskStatus != TaskStatuses.Completed
                         && _context.Projects.Any(p =>
                                p.UserId == teamLeadUserId && !p.IsDeleted && p.Id == t.ProjectId))
                .OrderBy(t => t.DueDate)
                .Take(take)
                .Select(t => new GetTaskResponse
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project != null ? t.Project.Name : string.Empty,
                    AssignedToUserId = t.AssignedToUserId,
                    AssignedToUserName = t.AssignedTo != null ? t.AssignedTo.Name : string.Empty,
                    AssignedByUserId = t.AssignedByUserId,
                    AssignedByUserName = t.AssignedBy != null ? t.AssignedBy.Name : string.Empty,
                    TaskStatus = t.TaskStatus,
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<GetProjectResponse>> GetActiveProjectsByTaskCountAsync(
            int teamLeadUserId, int take = 5)
        {
            return await _context.Projects
                .Where(p => p.UserId == teamLeadUserId
                         && !p.IsDeleted
                         && p.ProjectStatus == ProjectStatus.Active)
                .Select(p => new GetProjectResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    UserId = p.UserId,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    TaskCount = _context.Tasks.Count(t => t.ProjectId == p.Id && !t.IsDeleted),
                })
                .OrderByDescending(x => x.TaskCount)
                .Take(take)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<GetProjectResponse>> GetRecentlyCompletedProjectsAsync(
            int teamLeadUserId, int take = 5)
        {
            return await _context.Projects
                .Where(p => p.UserId == teamLeadUserId
                         && !p.IsDeleted
                         && p.ProjectStatus == ProjectStatus.Completed)
                .OrderByDescending(p => p.UpdatedAt)
                .Take(take)
                .Select(p => new GetProjectResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    UserId = p.UserId,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                })
                .AsNoTracking()
                .ToListAsync();
        }
    }
}