namespace TurfAuthAPI.Models
{
    public class LoginOTPVerifyRequest
    {
        public string Email { get; set; }
        public string OTP { get; set; }
    }
}
