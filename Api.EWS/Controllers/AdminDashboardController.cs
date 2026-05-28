using Application.EWS.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.EWS.Helpers;

namespace Api.EWS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/admin/dashboard")]
    public class AdminDashboardController(IAdminDashboardService adminDashboardService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var result = await adminDashboardService.GetDashboardAsync();
            return Ok(ResponseHelper.SuccessResponse(result, "Admin dashboard fetched successfully."));
        }
    }
}
