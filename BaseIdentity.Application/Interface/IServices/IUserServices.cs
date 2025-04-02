namespace BaseIdentity.Application.Interface.IServices
{
    public interface IUserServices
    {
        Task<ApiResult<UserResponse>> RegisterAsync(UserRegisterRequest request);
        Task<ApiResult<CurrentUserResponse>> GetCurrentUserAsync();
        Task<ApiResult<UserResponse>> GetByIdAsync(Guid id);
        Task<ApiResult<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest request);
        Task DeleteAsync(Guid id);
        Task<ApiResult<RevokeRefreshTokenResponse>> RevokeRefreshToken(RefreshTokenRequest refreshTokenRemoveRequest);
        Task<ApiResult<CurrentUserResponse>> RefreshTokenAsync(RefreshTokenRequest request);

        Task<ApiResult<UserResponse>> LoginAsync(UserLoginRequest request);
        Task<ApplicationUser> CreateOrUpdateGoogleUserAsync(GoogleUserInfo googleUserInfo);

    }
}
