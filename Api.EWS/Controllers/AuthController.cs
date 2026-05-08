using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Api.EWS.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] RegisterRequest request)
        {
            var response = await authService.RegisterAsync(request);
            if (response == null)
                return BadRequest("Registration failed.");

            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await authService.LoginAsync(request);
            if (response == null)
                return Unauthorized("Invalid email or password.");

            return Ok(response);
        }
    }
}
