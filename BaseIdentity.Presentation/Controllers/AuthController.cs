using BaseIdentity.Application.DTOs.Request;
using BaseIdentity.Application.Interface.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BaseIdentity.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserServices _userServices;

        public AuthController(IUserServices userServices)
        {
            _userServices = userServices;
        }
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
        {
            
            var reponse = await _userServices.RegisterAsync(request);
            return Ok(reponse);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            var reponse = await _userServices.LoginAsync(request);
            return Ok(reponse);
        }

        //get user by id
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id)
        {
            var reponse = await _userServices.GetByIdAsync(id);
            return Ok(reponse);
        }

        //refresh token
        [HttpPost("refresh-token")]
        [Authorize]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var reponse = await _userServices.RefreshTokenAsync(request);
            return Ok(reponse);
        }

        //revoke refresh token
        [HttpPost("revoke-refresh-token")]
        [Authorize]
        public async Task<IActionResult> RevokeRefreshToken([FromBody] RefreshTokenRequest request)
        {
            var response = await _userServices.RevokeRefreshToken(request);
            if (response.IsSuccess)
            {
                return Ok(response);
            }

             return BadRequest(response);
        }

        [HttpGet("current-user")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var response = await _userServices.GetCurrentUserAsync();
            return Ok(response);
        }
    }
}
