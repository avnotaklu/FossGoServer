using Microsoft.AspNetCore.Authorization;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using BadukServer.Dto;
using BadukServer.Services;

[ApiController]
[Route("[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly ILogger<AuthenticationController> _logger;
    private readonly UsersService _usersService;
    private readonly AuthenticationService _authenticationService;

    [ActivatorUtilitiesConstructorAttribute]
    public AuthenticationController(ILogger<AuthenticationController> logger, UsersService usersService, AuthenticationService authenticationService)
    {
        _logger = logger;
        _authenticationService = authenticationService;
        _usersService = usersService;
    }
    [AllowAnonymous]
    [HttpPost("GoogleSignIn")]
    public async Task<ActionResult<UserAuthenticationModel>> GoogleSignIn()
    {
        string token = Request.Headers["Authorization"].ToString().Remove(0, 7); //remove Bearer 
        try
        {
            var payload = await _authenticationService.VerifyGoogleTokenId(token);


            var email = payload.Email;
            var user = await _usersService.GetByEmail(email);

            if (user == null)
            {
                return await SignUp(new UserDetailsDto(email));
            }
            else {
                return await Login(new UserDetailsDto(email));
            }
        }
        catch (System.Exception e)
        {
            return BadRequest(e.ToString());
        }
    }

    private async Task<ActionResult<UserAuthenticationModel>> Login(UserDetailsDto request)
    {
        var user = await _usersService.GetByEmail(request.Email);
        if (user == null)
        {
            _logger.LogInformation("Incorrect email provided {email}", request.Email);
            return Unauthorized("User credentials don't match");
        }

        // bool validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);

        // if (!validPassword)
        // {

        //     _logger.LogInformation("Incorrect password provided {password}", request.Password);
        //     return Unauthorized("User credentials don't match");
        // }
        return Ok(new UserAuthenticationModel(user, _authenticationService.GenerateJSONWebToken(user.Id!)));
    }

    private async Task<ActionResult<UserAuthenticationModel>> SignUp(UserDetailsDto request)
    {
        var user = await _usersService.GetByEmail(request.Email);
        if (user != null)
        {
            return BadRequest("User already exists");
        }

        // string salt = BCrypt.Net.BCrypt.GenerateSalt();
        // string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, salt);
        var newUser = await _usersService.CreateUser(new UserDetailsDto(request.Email));

        if (newUser == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create user");
        }
        return Ok(new UserAuthenticationModel(newUser, _authenticationService.GenerateJSONWebToken(newUser.Id!)));
    }


    
//     private async Task<UserAuthenticationModel> Login(UserDetailsDto request)
//     {
//         var user = await _usersService.GetByEmail(request.Email);
//         if (user == null)
//         {
//             _logger.LogInformation("Incorrect email provided {email}", request.Email);
//             throw new BadukServerException(
//  "User credentials don't match", ""
//             );
//         }

//         // bool validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);

//         // if (!validPassword)
//         // {

//         //     _logger.LogInformation("Incorrect password provided {password}", request.Password);
//         //     return Unauthorized("User credentials don't match");
//         // }
//         return new UserAuthenticationModel(user, _authenticationService.GenerateJSONWebToken(user.Id!));
//     }

//     private async Task<UserAuthenticationModel> SignUp(UserDetailsDto request)
//     {
//         var user = await _usersService.GetByEmail(request.Email);
//         if (user != null)
//         {
//             throw new BadukServerException(
// "User already exists", ""
//             );
//         }

//         // string salt = BCrypt.Net.BCrypt.GenerateSalt();
//         // string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, salt);
//         var newUser = await _usersService.CreateUser(new UserDetailsDto(request.Email));

//         if (newUser == null)
//         {
//             throw new BadukServerException("Failed to create user", "");
//         }
//         return new UserAuthenticationModel(newUser, _authenticationService.GenerateJSONWebToken(newUser.Id!));
//     }
}