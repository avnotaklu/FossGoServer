public class AuthCreds
{
    public string Token { get; set; }   
    public string? RefreshToken { get; set; }

    public AuthCreds(string token, string? refreshToken)
    {
        Token = token;
        RefreshToken = refreshToken;
    }
}