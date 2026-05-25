using AutoMapper;
using Domain.EWS.DataModels.Response.Profile;
using Domain.EWS.DataModels.Response.Project;
using Domain.EWS.DataModels.Response.Tasks;
using Domain.EWS.DataModels.Response.User;
using Shared.EWS.Entities;

namespace Application.EWS.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Tasks, GetTaskResponse>()
                .ForMember(d => d.ProjectName, o => o.MapFrom(s => s.Project != null ? s.Project.Name : string.Empty))
                .ForMember(d => d.AssignedToUserName, o => o.MapFrom(s => s.AssignedTo != null ? s.AssignedTo.Name : string.Empty))
                .ForMember(d => d.AssignedByUserName, o => o.MapFrom(s => s.AssignedBy != null ? s.AssignedBy.Name : string.Empty))
                .ForMember(d => d.Comments, o => o.MapFrom(s => s.Comments
                    .Where(c => !c.IsDeleted)
                    .OrderBy(c => c.CreatedAt)
                    .ToList()))
                .ForMember(d => d.Attachments, o => o.MapFrom(s => s.Attachments
                    .Where(a => !a.IsDeleted)
                    .OrderBy(a => a.CreatedAt)
                    .ToList()));

            CreateMap<TaskComment, TaskCommentResponse>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User != null ? s.User.Name : string.Empty));

            CreateMap<TaskAttachment, TaskAttachmentResponse>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User != null ? s.User.Name : string.Empty));

            CreateMap<Projects, GetProjectResponse>();

            CreateMap<User, TeamLeaderResponse>()
                .ForMember(d => d.UserId, o => o.MapFrom(s => s.Id));

            CreateMap<User, GetUserResponse>()
                .ForMember(d => d.UserId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.status))
                .ForMember(d => d.RoleName, o => o.Ignore())
                .ForMember(d => d.TeamLeadName, o => o.Ignore());

            CreateMap<Role, RoleResponse>()
                .ForMember(d => d.RoleId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.RoleName, o => o.MapFrom(s => s.Name));

            CreateMap<User, GetProfileResponse>()
                .ForMember(d => d.UserId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.RoleName, o => o.Ignore());
        }
    }
}
