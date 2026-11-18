using System.ComponentModel.DataAnnotations;

namespace MovieMind.Application.Dtos.Ratings;

public class RateMovieRequest
{
    [Range(0, 10)]
    public double Score { get; set; }
}
