using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TurfAuthAPI.Models;
using TurfAuthAPI.Services;

namespace TurfAuthAPI.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupRequest request)
        {
            var result = await _authService.SignupAsync(request);
            if (result == "Email already registered")
                return Conflict(new { message = result });

            return Ok(new { message = result });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOTP([FromBody] OTPVerifyRequest request)
        {
            var result = await _authService.VerifySignupOTPAsync(request);
            if (result == "Invalid or expired OTP")
                return BadRequest(new { message = result });

            return Ok(new { message = result });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            if (result == "Invalid credentials")
                return Unauthorized(new { message = result });

            return Ok(new { message = result });
        }

        [HttpPost("login/verify-otp")]
        public async Task<IActionResult> VerifyLoginOTP([FromBody] OTPVerifyRequest request)
        {
            var (message, token, user) = await _authService.VerifyLoginOTPAsync(request);

            if (token == null)
                return BadRequest(new { message });

            return Ok(new
            {
                message,
                token,
                user = new
                {
                    name = user.Name,
                    email = user.Email,
                    role = user.Role
                }
            });
        }
    }
}
