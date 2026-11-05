using System;
using System.Collections.Generic;
using System.Text;

namespace MovieMind.Domain.Entities
{
    public class Movie
    {
        public Guid Id { get; set; }

        public string Title { get; set; } =string.Empty;
       
        public string Description { get; set; } =string.Empty;

        public int ReleaseYear { get; set; }

        public int Duration {  get; set; }

        public string PosterUrl { get; set; } =string.Empty;

        public double AverageRating { get; set; }

        public int RatingCount { get; set; }

        public string OriginalTitle { get; set; } = string.Empty;
        public string AgeRating { get; set; } = string.Empty;
        public double CatalogRating { get; set; }
        public string Director { get; set; } = string.Empty;
        public string Composer { get; set; } = string.Empty;
        public List<string> Writers { get; set; } = [];
        public string Awards { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string TrailerUrl { get; set; } = string.Empty;
        public int ArtPalette { get; set; }
        public string ArtMotif { get; set; } = "rings";
        public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public long UpdatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public ICollection<Rating> Ratings {  get; set; }=new List<Rating>();

        public ICollection<Genre> Genres { get; set; }=new List<Genre>();

        public ICollection<Actor> Actors { get; set; } = new List<Actor>();

        public ICollection<MovieActor> MovieActors { get; set; } = new List<MovieActor>();
    }
}
