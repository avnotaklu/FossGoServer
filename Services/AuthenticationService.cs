using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BadukServer;
using BadukServer.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public class AuthenticationService
{
    public JwtSettings JwtSettings;
    public AuthenticationService(
        IOptions<JwtSettings> jwtSettings
    )
    {
        JwtSettings = jwtSettings.Value;
    }


    public string GenerateJSONWebTokenForNormalUser(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSettings.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new ClaimsIdentity(new[] {
            new Claim("user_id", user.GetUserId()),
            new Claim("user_type", "normal_user"),
            new Claim("role", "player")
        });

        var token = new JwtSecurityToken(JwtSettings.Issuer,
          JwtSettings.Issuer,
          claims.Claims,
          expires: DateTime.Now.AddMinutes(120),
          signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }


    public string GenerateJSONWebTokenGuestUser(GuestUser user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSettings.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new ClaimsIdentity(new[] {
            new Claim("user_id", user.Id),
            new Claim("user_type", "guest_user"),
            new Claim("role", "player")
        });

        var token = new JwtSecurityToken(JwtSettings.Issuer,
          JwtSettings.Issuer,
          claims.Claims,
          expires: DateTime.Now.AddMinutes(120),
          signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<GoogleJsonWebSignature.Payload> VerifyGoogleTokenId(string token)
    {
        try
        {
            // uncomment these lines if you want to add settings: 
            // var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            // { 
            //     Audience = new string[] { "yourServerClientIdFromGoogleConsole.apps.googleusercontent.com" }
            // };
            // Add your settings and then get the payload
            // GoogleJsonWebSignature.Payload payload =  await GoogleJsonWebSignature.ValidateAsync(token, validationSettings);

            // Or Get the payload without settings.
            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(token);

            return payload;
        }
        catch (InvalidJwtException e)
        {
            if (e.Data != null)
            {
                var errorDetails = string.Join(Environment.NewLine, e.Data);
                throw new BadukServerException(e.Message, errorDetails);
            }
            else
            {
                throw new BadukServerException("invalid google token", "");
            }
        }
        catch (Exception)
        {
            throw new BadukServerException("invalid google token", "");
        }

    }
}