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

        public ICollection<Rating> Ratings {  get; set; }=new List<Rating>();

        public ICollection<Genre> Genres { get; set; }=new List<Genre>();

        public ICollection<Actor> Actors { get; set; } = new List<Actor>();

        public ICollection<MovieActor> MovieActors { get; set; } = new List<MovieActor>();
    }
}
