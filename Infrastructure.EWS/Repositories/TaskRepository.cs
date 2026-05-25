using Domain.EWS.DataModels.Request.Tasks;
using Domain.EWS.Interface;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
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
            {
                query = query.Where(t =>
                    EF.Functions.Like(t.Title, $"%{search}%"));
            }

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

        public async Task<IEnumerable<Projects>> GetProjectsByUserIdAsync(int userId)
            => await _context.Projects
                .Where(p => p.UserId == userId && !p.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
    }
}
