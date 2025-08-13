namespace TurfAuthAPI.Models
{
    public class OTPVerifyRequest
    {
        public string Email { get; set; }
        public string OTP { get; set; }
    }
}
