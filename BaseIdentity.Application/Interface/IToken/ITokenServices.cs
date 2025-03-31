using AuthenticationApi.Application.Common;
using BaseIdentity.Domain.Entities;
namespace BaseIdentity.Application.Interface.IToken
{
    public interface ITokenServices
    {
        Task<ApiResult<string>> GenerateToken(ApplicationUser user);
        string GenerateRefreshToken();
    }
}
