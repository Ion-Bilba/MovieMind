using System;
using System.Collections.Generic;
using System.Text;

namespace MovieMind.Domain.Entities
{
    public class Actor
    {
        public Guid Id { get; set; }

        public string Name { get; set; } =string.Empty;


        public ICollection<Movie> Movies {  get; set; }=new List<Movie>();

        public ICollection<MovieActor>MovieActors { get; set; } =new List<MovieActor>();
    }
}
