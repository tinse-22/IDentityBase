namespace BaseIdentity.Application.Interface.IExternalAuthService
{
    public interface IExternalAuthService
    {
        Task<ApiResult<string>> ProcessGoogleLoginAsync();

    }
}
