using Domain.EWS.DataModels.Request.Project;
using Domain.EWS.DataModels.Response.Project;
using Domain.EWS.Interface;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Extensions;

namespace Infrastructure.EWS.Repositories
{
    public class ProjectRepository(EWSDbContext context)
        : GenericRepository<Projects>(context), IProjectRepository
    {
        public async Task<PagedResponse<GetProjectResponse>> GetAllProjectsAsync(ProjectSearchRequest request)
        {
            var query = _context.Projects
                .Where(p => !p.IsDeleted)
                .Select(p => new GetProjectResponse
                {
                    Id            = p.Id,
                    Name          = p.Name,
                    Description   = p.Description,
                    UserId        = p.UserId,
                    ProjectStatus = p.ProjectStatus,
                    StartDate     = p.StartDate,
                    EndDate       = p.EndDate
                })
                .AsNoTracking();

            var search = request.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p =>
                    EF.Functions.ILike(p.Name, $"%{search}%"));
            }

            if (request.ProjectStatus.HasValue)
            {
                query = query.Where(p => p.ProjectStatus == request.ProjectStatus.Value);
            }

            if (request.TeamLeadId.HasValue)
            {
                query = query.Where(p => p.UserId == request.TeamLeadId.Value);
            }

            if (request.StartDateFrom.HasValue)
            {
                query = query.Where(p => p.StartDate >= request.StartDateFrom.Value.ToUniversalTime());
            }

            if (request.EndDateTo.HasValue)
            {
                query = query.Where(p => p.EndDate <= request.EndDateTo.Value.ToUniversalTime().AddDays(1).AddSeconds(-1));
            }

            return await query.ToPagedResponseAsync(request);
        }

        public async Task<bool> ProjectNameExistsAsync(string name, Guid? excludeId = null)
        {
            var query = _context.Projects
                .Where(p => p.Name.ToLower() == name.ToLower() && !p.IsDeleted);

            if (excludeId.HasValue)
                query = query.Where(p => p.Id != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<User?> GetUserWithRoleAsync(int userId)
            => await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        public async Task<IEnumerable<TeamLeaderResponse>> GetTeamLeadersAsync()
        {
            return await _context.Users
                .Where(u => u.RoleId == 2 && !u.IsDeleted && u.status == true)
                .AsNoTracking()
                .Select(u => new TeamLeaderResponse
                {
                    UserId = u.Id,
                    Name   = u.Name
                })
                .ToListAsync();
        }
    }
}