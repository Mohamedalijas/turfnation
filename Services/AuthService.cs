using MongoDB.Driver;
using Microsoft.Extensions.Options;
using TurfAuthAPI.Config;
using TurfAuthAPI.Models;
using TurfAuthAPI.Services;
using BCrypt.Net;
using System;
using System.Threading.Tasks;

namespace TurfAuthAPI.Services
{
    public class AuthService
    {
        private readonly IMongoCollection<User> _users;
        private readonly EmailService _emailService;
        private readonly TokenService _tokenService;

        public AuthService(
            IOptions<MongoDbSettings> mongoSettings, 
            EmailService emailService,
            TokenService tokenService)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var db = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _users = db.GetCollection<User>("Users");
            _emailService = emailService;
            _tokenService = tokenService;
        }

        public async Task<string> SignupAsync(SignupRequest request)
        {
            var existingUser = await _users.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
            if (existingUser != null)
                return "Email already registered";

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            string otp = new Random().Next(100000, 999999).ToString();

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = hashedPassword,
                Phone = request.Phone,
                Role = request.Role,
                Status = "pending",
                OTP = otp,
                OTPExpiry = DateTime.UtcNow.AddMinutes(1)
            };

            await _users.InsertOneAsync(user);

            await _emailService.SendEmailAsync(request.Email, "OTP Verification", $"Your OTP is: {otp}");

            return "OTP sent to registered email for verification";
        }

        public async Task<string> VerifySignupOTPAsync(OTPVerifyRequest request)
        {
            var user = await _users.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
            if (user == null || user.OTP != request.OTP || DateTime.UtcNow > user.OTPExpiry)
                return "Invalid or expired OTP";

            var update = Builders<User>.Update
                .Set(u => u.Status, "active")
                .Set(u => u.OTP, null)
                .Set(u => u.OTPExpiry, null);

            await _users.UpdateOneAsync(u => u.Email == request.Email, update);

            return "Account verified successfully";
        }

        // New Login method: validates password, generates OTP, sends email
        public async Task<string> LoginAsync(LoginRequest request)
        {
            var user = await _users.Find(u => u.Email == request.Email && u.Status == "active").FirstOrDefaultAsync();
            if (user == null)
                return "Invalid credentials";

            bool passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!passwordValid)
                return "Invalid credentials";

            string otp = new Random().Next(100000, 999999).ToString();

            var update = Builders<User>.Update
                .Set(u => u.OTP, otp)
                .Set(u => u.OTPExpiry, DateTime.UtcNow.AddMinutes(1));

            await _users.UpdateOneAsync(u => u.Email == request.Email, update);

            await _emailService.SendEmailAsync(request.Email, "Login OTP", $"Your login OTP is: {otp}");

            return "OTP sent to registered email for verification";
        }

        // New Verify Login OTP method: check OTP, generate JWT token if valid
        public async Task<(string message, string token, User user)> VerifyLoginOTPAsync(OTPVerifyRequest request)
        {
            var user = await _users.Find(u => u.Email == request.Email && u.Status == "active").FirstOrDefaultAsync();
            if (user == null || user.OTP != request.OTP || DateTime.UtcNow > user.OTPExpiry)
                return ("Invalid or expired OTP", null, null);

            var update = Builders<User>.Update
                .Set(u => u.OTP, null)
                .Set(u => u.OTPExpiry, null);

            await _users.UpdateOneAsync(u => u.Email == request.Email, update);

            var token = _tokenService.GenerateToken(user.Id.ToString(), user.Name, user.Email, user.Role);

            return ("Login successful", token, user);
        }
    }
}
