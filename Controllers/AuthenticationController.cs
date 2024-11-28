using Microsoft.AspNetCore.Authorization;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using BadukServer.Dto;
using BadukServer.Services;
using System.ComponentModel.DataAnnotations;
using System.Runtime.ConstrainedExecution;

[ApiController]
[Authorize]
[Route("[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly ILogger<AuthenticationController> _logger;
    private readonly IUsersService _usersService;
    private readonly IUserRatingService _userRatingService;
    private readonly AuthenticationService _authenticationService;

    [ActivatorUtilitiesConstructorAttribute]
    public AuthenticationController(ILogger<AuthenticationController> logger, IUsersService usersService, IUserRatingService userRatingService, AuthenticationService authenticationService)
    {
        _logger = logger;
        _authenticationService = authenticationService;
        _usersService = usersService;
        _userRatingService = userRatingService;
    }

    [AllowAnonymous]
    [HttpPost("GoogleSignIn")]
    public async Task<ActionResult<UserAuthenticationModel>> GoogleSignIn([FromBody] GoogleSignInTokenBody tokenBody)
    {
        string token = tokenBody.Token; //remove Bearer 
        try
        {
            var payload = await _authenticationService.VerifyGoogleTokenId(token);


            var email = payload.Email;
            var user = await _usersService.GetByEmail(email);

            if (user == null)
            {
                return await SignUp(new UserDetailsDto(email, true));
            }
            else
            {
                return await Login(new UserDetailsDto(email, true));
            }
        }
        catch (System.Exception e)
        {
            return BadRequest(e.ToString());
        }
    }

    [AllowAnonymous]
    [HttpPost("PasswordLogIn")]
    public async Task<ActionResult<UserAuthenticationModel>> PasswordLogIn([FromBody] UserDetailsDto userDetails)
    {
        try
        {
            return await Login(userDetails);
        }
        catch (System.Exception e)
        {
            return BadRequest(e.ToString());
        }
    }


    [AllowAnonymous]
    [HttpPost("PasswordSignUp")]
    public async Task<ActionResult<UserAuthenticationModel>> PasswordSignUp([FromBody] UserDetailsDto userDetails)
    {
        try
        {
            return await SignUp(userDetails);
        }
        catch (Exception e)
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

        if (!request.GoogleSignIn)
        {
            bool validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!validPassword)
            {
                _logger.LogInformation("Incorrect password provided {password}", request.Password);
                return Unauthorized("User credentials don't match");
            }
        }

        _logger.LogInformation("Signup successful {email}", request.Email);
        return Ok(new UserAuthenticationModel(user, _authenticationService.GenerateJSONWebToken(user.Id!)));
    }

    private async Task<ActionResult<UserAuthenticationModel>> SignUp(UserDetailsDto request)
    {
        var user = await _usersService.GetByEmail(request.Email);
        if (user != null)
        {

            _logger.LogInformation("User already exists {email}", request.Email);
            return BadRequest("User already exists");
        }

        string? password = null;
        if (!request.GoogleSignIn)
        {
            if (request.Password == null)
            {

                _logger.LogInformation("No password provided");
                return BadRequest("Please provide a password");
            }
            string salt = BCrypt.Net.BCrypt.GenerateSalt();
            password = BCrypt.Net.BCrypt.HashPassword(request.Password, salt);
        }
        var newUser = await _usersService.CreateUser(request.Email, request.GoogleSignIn, password);
        var newRatings = await _userRatingService.CreateUserRatings(newUser!.Id!);

        if (newUser == null)
        {

            _logger.LogInformation("Couldn't create user");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create user");
        }

        _logger.LogInformation("Signup successful {email}", newUser.Email);
        return Ok(new UserAuthenticationModel(newUser, _authenticationService.GenerateJSONWebToken(newUser.Id!)));
    }


    [HttpGet("GetUser")]
    public async Task<ActionResult<UserAuthenticationModel>> GetUser()
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();


        var users = await _usersService.GetByIds([userId]);

        if (users.Count == 0)
        {
            _logger.LogInformation("User doesn't exist");
            return Unauthorized("You don't exist");
        }

        var user = users.First();

        return Ok(new UserAuthenticationModel(user, _authenticationService.GenerateJSONWebToken(user.Id!)));
    }
}