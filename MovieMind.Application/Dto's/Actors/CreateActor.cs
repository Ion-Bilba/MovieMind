using System.ComponentModel.DataAnnotations;

namespace MovieMind.Application.Dtos.Actors;

public class CreateActor
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}