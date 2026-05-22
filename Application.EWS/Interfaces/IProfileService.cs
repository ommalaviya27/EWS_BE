using Domain.EWS.DataModels.Request.Profile;
using Domain.EWS.DataModels.Response.Profile;

namespace Application.EWS.Interfaces
{
    public interface IProfileService
    {
        Task<GetProfileResponse> GetProfileAsync(int userId);
        Task<GetProfileResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request);
        Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
    }
}
