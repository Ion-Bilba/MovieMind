using System;
using System.Collections.Generic;
using System.Text;

namespace MovieMind.Domain.Entities
{
    public class Rating
    {
        public Guid Id { get; set; }

        public Guid UserId {  get; set; }

        public User User { get; set; } = null!;

        public Guid MovieId { get; set; }

        public Movie Movie { get; set; } = null!;

        public int Score { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set;}

        
    }
}
