using Domain.EWS.DataModels.Request.Profile;
using Domain.EWS.DataModels.Response.Profile;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces;

namespace Application.EWS.Interfaces
{
    public interface IProfileService : IGenericService<User>
    {
        Task<GetProfileResponse> GetProfileAsync();
        Task<GetProfileResponse> UpdateProfileAsync(UpdateProfileRequest request);
        Task ChangePasswordAsync(ChangePasswordRequest request);
    }
}