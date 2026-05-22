using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.EWS.Helpers;

namespace Api.EWS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/profile")]
    public class ProfileController(IProfileService profileService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var result = await profileService.GetProfileAsync(GetCallerId());
            return Ok(ResponseHelper.SuccessResponse(result, "Profile fetched successfully."));
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var result = await profileService.UpdateProfileAsync(GetCallerId(), request);
            return Ok(ResponseHelper.SuccessResponse(result, "Profile updated successfully."));
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            await profileService.ChangePasswordAsync(GetCallerId(), request);
            return Ok(ResponseHelper.SuccessResponse<object>(
                null,
                "Password changed successfully. Please log in again."));
        }

        private int GetCallerId()
            => int.TryParse(User.FindFirst("user_id")?.Value, out var id) ? id : 0;
    }
}
