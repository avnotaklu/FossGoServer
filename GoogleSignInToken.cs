using BadukServer.Dto;

public class GoogleSignInTokenBody
{
    public string Token { get; set; }

    public GoogleSignInTokenBody(string token)
    {
        Token = token;
    }
    
}