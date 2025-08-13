using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace TurfAuthAPI.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public string TurfName { get; set; }
        public string Location { get; set; }
        
        public string Status { get; set; } = "pending"; // pending or active
        public string OTP { get; set; }
        public DateTime? OTPExpiry { get; set; }

    }
}
