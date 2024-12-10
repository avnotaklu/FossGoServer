
using System.ComponentModel.DataAnnotations;

public class UpdateProfileDto
{
    [RegularExpression(@"^[\p{L} .'-]+$", ErrorMessage = "Full name can only contain unicode characters, spaces, apostrophes, and hyphens.")]
    public string? FullName { get; set; }
    [MinLength(10)]
    [MaxLength(100)]
    public string? Bio { get; set; }
    [RegularExpression(@"^[A-Z][A-Z]$", ErrorMessage = "Country code is incorrect")]
    public string? Nationality { get; set; }

    public UpdateProfileDto(string? fullName, string? bio, string nationality)
    {
        FullName = fullName;
        Bio = bio;
        Nationality = nationality;
    }
}