using System.ComponentModel.DataAnnotations;

namespace MovieMind.Application.Dtos.Genres;

public class CreateGenreRequest
{
    [Required(ErrorMessage = "Numele genului este obligatoriu.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Numele trebuie să aibă între 2 și 100 de caractere.")]
    public string Name { get; set; } = string.Empty;
}