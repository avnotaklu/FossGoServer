
using System.ComponentModel.DataAnnotations;

public class UpdateProfileDto
{
    [RegularExpression(@"^[\p{L} .'-]+$", ErrorMessage = "Full name can only contain unicode characters, spaces, apostrophes, and hyphens.")]
    public string? FullName { get; set; }

    [MinLength(10, ErrorMessage = "Bio must be at least 10 characters long")]
    [MaxLength(100, ErrorMessage = "Bio must be at most 100 characters long")]
    public string? Bio { get; set; }
    [RegularExpression(@"^[A-Z][A-Z]$", ErrorMessage = "Country code is incorrect")]
    [MinLength(2, ErrorMessage = "Nationality must be 2 digit Country Code")]
    public string? Nationality { get; set; }

    public UpdateProfileDto(string? fullName, string? bio, string nationality)
    {
        FullName = fullName;
        Bio = bio;
        Nationality = nationality;
    }
}