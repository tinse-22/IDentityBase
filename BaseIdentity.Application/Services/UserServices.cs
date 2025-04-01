using System.Security.Cryptography;
using System.Text;
using System.Transactions;
using AuthenticationApi.Application.Common;
using AutoMapper;
using BaseIdentity.Application.DTOs.Identities;
using BaseIdentity.Application.DTOs.Request;
using BaseIdentity.Application.DTOs.Response;
using BaseIdentity.Application.Interface.IServices;
using BaseIdentity.Application.Interface.IToken;
using BaseIdentity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BaseIdentity.Application.Services
{
    public class UserServices : IUserServices
    {
        //private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenServices _tokenServices;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<UserServices> _logger;
        
        public UserServices(ITokenServices tokenServices, ICurrentUserService currentUserService, UserManager<ApplicationUser> userManager, IMapper mapper, ILogger<UserServices> logger)
        {
            /*IUnitOfWork unitOfWork, */
            //_unitOfWork = unitOfWork;
            _tokenServices = tokenServices;
            _currentUserService = currentUserService;
            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResult<UserResponse>> RegisterAsync(UserRegisterRequest request)
        {
            _logger.LogInformation("Register User");
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogInformation("Email already exists");
                return ApiResult<UserResponse>.Failure("Email already exists");
            }

            var newUser = _mapper.Map<ApplicationUser>(request);
            newUser.UserName = GenerateUserName(request.FirstName, request.LastName);

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                // Tạo user
                var createResult = await _userManager.CreateAsync(newUser, request.Password);
                if (!createResult.Succeeded)
                {
                    _logger.LogInformation("Register failed");
                    return ApiResult<UserResponse>.Failure(string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }

                // Gán role mặc định "USER"
                var roleResult = await _userManager.AddToRoleAsync(newUser, "USER");
                if (!roleResult.Succeeded)
                {
                    _logger.LogInformation("Assign role failed");
                    return ApiResult<UserResponse>.Failure(string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }

                // Nếu tất cả thành công, hoàn thành giao dịch
                scope.Complete();
            }

            _logger.LogInformation("User create successful");
            await _tokenServices.GenerateToken(newUser);
            var userResponse = _mapper.Map<UserResponse>(newUser);
            return ApiResult<UserResponse>.Success(userResponse);
        }

        //This method generates a unique username by concatenating the first name and last name. If the username already exists, it appends a number to the username until it finds a unique one.

        private string GenerateUserName(string firstName, string lastName)
        {
            // Loại bỏ khoảng trắng trong firstName và lastName
            var normalizedFirstName = firstName.Replace(" ", string.Empty);
            var normalizedLastName = lastName.Replace(" ", string.Empty);
            var baseUsername = $"{normalizedFirstName}{normalizedLastName}".ToLower();
            var userName = baseUsername;
            var count = 1;
            while (_userManager.Users.Any(u => u.UserName == userName))
            {
                userName = $"{baseUsername}{count}";
                count++;
            }
            return userName;
        }


        // A method to login a user
        public async Task<ApiResult<UserResponse>> LoginAsync(UserLoginRequest request)
        {
            if (request == null)
            {
                _logger.LogInformation("Login request is null!!!");
                return ApiResult<UserResponse>.Failure("Invalid request");
            }
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                _logger.LogInformation("Invalid user or password");
                return ApiResult<UserResponse>.Failure("Invalid user or password");
            }

            //Generate access token
            var accessToken = await _tokenServices.GenerateToken(user);

            //Generate refresh token
            var refreshToken = _tokenServices.GenerateRefreshToken();

            //Hash the refresh token and store it in the database or override the existing refresh token
            using var sha256 = SHA256.Create();
            var hashedRefreshToken = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
            user.RefreshToken = Convert.ToBase64String(hashedRefreshToken);
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            //Update USER information in database

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogInformation("Login failed");
                return ApiResult<UserResponse>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            var userResponse = _mapper.Map<ApplicationUser, UserResponse>(user);
            userResponse.AccessToken = accessToken.Data;
            userResponse.RefreshToken = refreshToken;

            return ApiResult<UserResponse>.Success(userResponse);
        }

        //Get user by id
        public async Task<ApiResult<UserResponse>> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Get user by id");
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                _logger.LogInformation("User not found");
                return ApiResult<UserResponse>.Failure("User not found");
            }
            _logger.LogInformation("User found");
            var userResponse = _mapper.Map<UserResponse>(user);
            return ApiResult<UserResponse>.Success(userResponse);
        }

        //Get current user
        public async Task<ApiResult<CurrentUserResponse>> GetCurrentUserAsync()
        {
            var user =await _userManager.FindByIdAsync(_currentUserService.GetUserId());
            if (user == null)
            {
                _logger.LogInformation("User not found");
                return ApiResult<CurrentUserResponse>.Failure("User not found");
            }
            var userResponse = _mapper.Map<CurrentUserResponse>(user);
            return ApiResult<CurrentUserResponse>.Success(userResponse);
        }

        //Revoke refresh token
        //public async Task<ApiResult<RevokeRefreshTokenResponse>> RevokeRefreshToken(RefreshTokenRequest refreshTokenRemoveRequest)
        //{
        //    _logger.LogInformation("Revoke refresh token");

        //    //Hash the incoming refresh token and compare it with the one stored in the database
        //    using var sha256 = SHA256.Create(); 
        //    var refreshTokenHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshTokenRemoveRequest.RefreshToken));
        //    var hashedRefreshToken = Convert.ToBase64String(refreshTokenHash);

        //    //find the user based on the refresh token
        //    var user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == hashedRefreshToken);
        //    if (user == null)
        //    {
        //        _logger.LogInformation("User not found");
        //        return ApiResult<RevokeRefreshTokenResponse>.Failure("User not found");
        //    }

        //    // validate the refresh token expiry time
        //    if(user.RefreshTokenExpiryTime < DateTime.UtcNow)
        //    {
        //        _logger.LogWarning("Refresh token expired");
        //        return ApiResult<RevokeRefreshTokenResponse>.Failure("Refresh token expired");
        //    }

        //    //generate a new refresh token
        //    var newAccessToken = _tokenServices.GenerateRefreshToken();

        //    var currentUserReponse = _mapper.Map<CurrentUserResponse>(user);
        //    currentUserReponse.AccessToken = newAccessToken;


        //}

        //Revoke refresh token
        public async Task<ApiResult<RevokeRefreshTokenResponse>> RevokeRefreshToken(RefreshTokenRequest refreshTokenRemoveRequest)
        {
            try
            {
                // Hash refresh token
                var hashedRefreshToken = ComputeSha256Hash(refreshTokenRemoveRequest.RefreshToken);

                // Find user based on the refresh token
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == hashedRefreshToken);
                if (user == null)
                {
                    _logger.LogInformation("User not found");
                    return ApiResult<RevokeRefreshTokenResponse>.Failure("User not found");
                }
                if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
                {
                    _logger.LogInformation("Refresh token expired");
                    return ApiResult<RevokeRefreshTokenResponse>.Failure("Refresh token expired");
                }

                // Remove the refresh token
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;

                // Update user information in the database
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    _logger.LogError("Revoke refresh token failed");
                    return ApiResult<RevokeRefreshTokenResponse>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                _logger.LogInformation("Refresh token revoked successfully");
                return ApiResult<RevokeRefreshTokenResponse>.Success(new RevokeRefreshTokenResponse { Message = "Refresh token revoked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while revoking the refresh token");
                throw;
            }
        }


        // Private helper method to compute the SHA256 hash
        private static string ComputeSha256Hash(string token)
        {
            using var sha256 = SHA256.Create();
            var tokenBytes = Encoding.UTF8.GetBytes(token);
            var hashBytes = sha256.ComputeHash(tokenBytes);
            return Convert.ToBase64String(hashBytes);
        }

        //Update user information
        public async Task<ApiResult<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest request)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                _logger.LogInformation("User not found");
                return ApiResult<UserResponse>.Failure("User not found");
            }
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Email = request.Email;
            user.Gender = request.Gender;

            await _userManager.UpdateAsync(user);
            var userResponse = _mapper.Map<UserResponse>(user);
            return ApiResult<UserResponse>.Success(userResponse);
        }

        public async Task<ApiResult<CurrentUserResponse>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            _logger.LogInformation("RefreshToken");
            var hashedRefreshToken = ComputeSha256Hash(request.RefreshToken);

            // Find user based on the refresh token
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == hashedRefreshToken);
            if (user == null)
            {
                _logger.LogError("Invalid refresh token");
                throw new Exception("Invalid refresh token");
            }

            // Validate the refresh token expiry time
            if (user.RefreshTokenExpiryTime < DateTime.Now)
            {
                _logger.LogWarning("Refresh token expired for user ID: {UserId}", user.Id);
                throw new Exception("Refresh token expired");
            }

            // Generate a new access token
            var newAccessToken = await _tokenServices.GenerateToken(user);
            _logger.LogInformation("Access token generated successfully");
            var currentUserResponse = _mapper.Map<CurrentUserResponse>(user);
            currentUserResponse.AccessToken = newAccessToken.Data;
            return ApiResult<CurrentUserResponse>.Success(currentUserResponse);
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                _logger.LogInformation("User not found");
                return;
            }
            await _userManager.DeleteAsync(user);
        }
        public async Task<ApplicationUser> CreateOrUpdateGoogleUserAsync(GoogleUserInfo googleUserInfo)
        {
            // Kiểm tra xem user đã tồn tại chưa
            var user = await _userManager.FindByEmailAsync(googleUserInfo.Email);

            if (user == null)
            {
                // Tạo mới user nếu chưa có
                user = new ApplicationUser
                {
                    UserName = googleUserInfo.Email,
                    Email = googleUserInfo.Email,
                    FirstName = googleUserInfo.FirstName,
                    LastName = googleUserInfo.LastName
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    // Handle error
                }

                // Gán role mặc định "USER"
                var roleResult = await _userManager.AddToRoleAsync(user, "USER");
                if (!roleResult.Succeeded)
                {
                    // Handle gán role thất bại
                }
            }
            else
            {
                // Nếu user đã có, cập nhật thông tin nếu có thay đổi
                bool hasChange = false;
                if (user.FirstName != googleUserInfo.FirstName)
                {
                    user.FirstName = googleUserInfo.FirstName;
                    hasChange = true;
                }
                if (user.LastName != googleUserInfo.LastName)
                {
                    user.LastName = googleUserInfo.LastName;
                    hasChange = true;
                }

                if (hasChange)
                {
                    var updateResult = await _userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        // Handle error
                    }
                }

                // Kiểm tra nếu người dùng chưa có role "USER", thì thêm vào
                if (!await _userManager.IsInRoleAsync(user, "USER"))
                {
                    var roleResult = await _userManager.AddToRoleAsync(user, "USER");
                    if (!roleResult.Succeeded)
                    {
                        // Handle error
                    }
                }
            }

            return user;
        }


    }
}
