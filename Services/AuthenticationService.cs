using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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

    public ClaimsPrincipal GetPrincipalFromExpiredToken(String token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false, //you might want to validate the audience and issuer depending on your use case
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSettings.Key)),
            ValidateLifetime = false //here we are saying that we don't care about the token's expiration date
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken securityToken;
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
        var jwtSecurityToken = securityToken as JwtSecurityToken;
        if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }


    public Task<string> GenerateJSONWebTokenForNormalUser(User user)
    {
        var claims = new ClaimsIdentity(new[] {
            new Claim("user_id", user.GetUserId()),
            new Claim("user_type", "normal_user"),
            new Claim(ClaimTypes.Role,"player"),
        });

        return _GenerateJSONWeb(claims);
    }

    public Task<string> GenerateJSONWebTokenGuestUser(GuestUser user)
    {

        var claims = new ClaimsIdentity([
            new Claim("user_id", user.Id),
            new Claim("user_type", "guest_user"),
            new Claim(ClaimTypes.Role,"player"),
        ]);

        return _GenerateJSONWeb(claims);
    }

    public Task<string> GenerateJSONWebTokenNewOAuthUser(string email)
    {
        var claims = new ClaimsIdentity([
            new Claim(ClaimTypes.Role, "new_oauth"),
            new Claim(ClaimTypes.Email ,email),
        ]);

        return _GenerateJSONWeb(claims);
    }


    public Task<string> _GenerateJSONWeb(ClaimsIdentity claims)
    {
        return Task.Run(() =>
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSettings.Key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(JwtSettings.Issuer,
              JwtSettings.Issuer,
              claims.Claims,
              expires: DateTime.UtcNow.AddMinutes(120),
              signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        });
    }

    public Task<string> GenerateRefreshToken()
    {
        return Task.Run(() =>
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSettings.Key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(JwtSettings.Issuer,
              JwtSettings.Issuer,
              [],
              expires: DateTime.UtcNow.AddMonths(1),
              signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        });
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