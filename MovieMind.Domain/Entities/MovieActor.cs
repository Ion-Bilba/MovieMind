using System;
using System.Collections.Generic;
using System.Text;

namespace MovieMind.Domain.Entities
{
    public class MovieActor
    {
        public Guid ActorId { get; set; }
        public Actor Actor { get; set; } = null!;

        public Guid MovieId { get; set; }
        public Movie Movie { get; set; }=null!;

        public string? CharacterName { get; set; }


    }
}
