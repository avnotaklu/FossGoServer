public class SignInDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? GoogleToken { get; set; }
    public string? Password { get; set; }

    public SignInDto(string username, string email, string password, string? googleToken)
    {
        Username = username;
        Email = email;
        Password = password;
        GoogleToken = googleToken;
    }
}