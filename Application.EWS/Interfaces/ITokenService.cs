using Shared.EWS.Entities;

namespace Application.EWS.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}