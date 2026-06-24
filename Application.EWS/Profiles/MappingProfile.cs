using AutoMapper;
using Domain.EWS.DataModels.Response.Attendance;
using Domain.EWS.DataModels.Response.PublicHoliday;
using Domain.EWS.DataModels.Response.Leave;
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
                .ForMember(d => d.AssignedToUserName, o => o.MapFrom(s => s.AssignedTo != null ? s.AssignedTo.Name : string.Empty));

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
                .ForMember(d => d.ReportingName, o => o.Ignore());

            CreateMap<Role, RoleResponse>()
                .ForMember(d => d.RoleId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.RoleName, o => o.MapFrom(s => s.Name));

            CreateMap<User, GetProfileResponse>()
                .ForMember(d => d.UserId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.RoleName, o => o.Ignore());

            CreateMap<Attendance, AttendanceResponse>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User != null ? s.User.Name : string.Empty));

            CreateMap<LeaveApplication, LeaveResponse>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User != null ? s.User.Name : string.Empty))
                .ForMember(d => d.CanEdit, o => o.Ignore())
                .ForMember(d => d.CanDelete, o => o.Ignore());

            CreateMap<PublicHoliday, HolidayResponse>();
        }
    }
}