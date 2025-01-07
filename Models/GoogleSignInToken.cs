using BadukServer.Dto;

[Immutable, GenerateSerializer]
[Alias("GoogleSignInTokenBody")]
public class GoogleSignInBody
{
    public string Token { get; set; }

    public GoogleSignInBody(string token)
    {
        Token = token;
    }

}

[Immutable, GenerateSerializer]
[Alias("GoogleSignUpTokenBody")]
public class GoogleSignUpBody
{
    public string Username { get; set; }

    public GoogleSignUpBody(string username)
    {
        Username = username;
    }

}

[Immutable, GenerateSerializer]
[Alias("GoogleSignUpResponse")]
public class GoogleSignInResponse
{
    public bool Authenticated { get; set; }
    public string? NewOAuthToken { get; set; }
    public UserAuthenticationModel? Auth { get; set; }

    public GoogleSignInResponse(bool authenticated, string? token, UserAuthenticationModel? auth)
    {
        Authenticated = authenticated;
        NewOAuthToken = token;
        Auth = auth;
    }
}