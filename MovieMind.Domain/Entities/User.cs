using MovieMind.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace MovieMind.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
       
        public string UserName { get; set; } = string.Empty;    

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.User;

        public DateTime CreatedAt { get; set; }= DateTime.UtcNow;

        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    }
}