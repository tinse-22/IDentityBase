using Microsoft.AspNetCore.Identity;

namespace BaseIdentity.Application.Services
{
    public class ExternalAuthService : IExternalAuthService
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenServices _tokenServices;

        public ExternalAuthService(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ITokenServices tokenServices)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _tokenServices = tokenServices;
        }

        public async Task<ApiResult<string>> ProcessGoogleLoginAsync()
        {
            // Lấy thông tin từ Google
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return ApiResult<string>.Failure("Không lấy được thông tin từ Google.");

            // Thử đăng nhập bằng external login
            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false);

            ApplicationUser user = null;
            if (!signInResult.Succeeded)
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                // Kiểm tra xem user đã tồn tại hay chưa
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser == null)
                {
                    // Tạo user mới nếu chưa tồn tại
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email
                    };
                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                        return ApiResult<string>.Failure(string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
                else
                {
                    user = existingUser;
                }

                // Thêm external login cho user nếu chưa có
                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (!addLoginResult.Succeeded)
                    return ApiResult<string>.Failure(string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));

                // Đăng nhập user
                await _signInManager.SignInAsync(user, isPersistent: false);
            }
            else
            {
                // Nếu đăng nhập thành công, lấy user đã liên kết
                user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            }

            // Tạo token JWT cho user
            var tokenResult = await _tokenServices.GenerateToken(user);
            return tokenResult;
        }
    }

}
