using Domain.EWS.DataModels.Request.Authentication;
using Domain.EWS.DataModels.Response.Authentication;

namespace Application.EWS.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
    }
}