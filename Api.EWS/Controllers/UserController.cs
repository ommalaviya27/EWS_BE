using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.EWS.Helpers;
using System.Net;

namespace Api.EWS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/users")]
    public class UserController(IUserService userService) : ControllerBase
    {
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var result = await userService.GetRolesAsync(GetCallerRoleId());
            return Ok(ResponseHelper.SuccessResponse(result, "Roles fetched successfully."));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] UserPaginationRequest pagination)
        {
            var result = await userService.GetAllUsersAsync(GetCallerRoleId(), pagination);
            return Ok(ResponseHelper.SuccessResponse(result, "Users fetched successfully."));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await userService.GetUserByIdAsync(id, GetCallerRoleId());
            return Ok(ResponseHelper.SuccessResponse(result, "User fetched successfully."));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            var result = await userService.CreateUserAsync(request, GetCallerRoleId());
            return CreatedAtAction(nameof(GetById), new { id = result.UserId },
                ResponseHelper.SuccessResponse(result, "User created successfully.", HttpStatusCode.Created));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
        {
            var result = await userService.UpdateUserAsync(id, request, GetCallerRoleId());
            return Ok(ResponseHelper.SuccessResponse(result, "User updated successfully."));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await userService.DeleteUserAsync(id, GetCallerRoleId());
            return Ok(ResponseHelper.SuccessResponse<object>(null, "User deleted successfully."));
        }

        private int GetCallerRoleId()
            => int.TryParse(User.FindFirst("role_id")?.Value, out var roleId) ? roleId : 0;
    }
}