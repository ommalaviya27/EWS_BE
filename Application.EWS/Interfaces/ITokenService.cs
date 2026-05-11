using Shared.EWS.Entities;

namespace Application.EWS.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
    }
}