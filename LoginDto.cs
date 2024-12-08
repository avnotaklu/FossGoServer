using System.ComponentModel.DataAnnotations;

public class LoginDto
{
    [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9_]+$", ErrorMessage = "Only letters, numbers and underscore are allowed for username, first character must be a letter.")]
    public string? Username { get; set; }
    [EmailAddress]
    public string? Email { get; set; }
    public string? GoogleToken { get; set; }
    [MinLength(6)]
    public string? Password { get; set; }

    public LoginDto(string? username, string? email, string? password, string? googleToken)
    {
        Username = username;
        Email = email;
        Password = password;
        GoogleToken = googleToken;
    }
}