using Microsoft.AspNetCore.Authorization;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using BadukServer.Dto;
using BadukServer.Services;
using System.ComponentModel.DataAnnotations;
using System.Runtime.ConstrainedExecution;
using MongoDB.Bson;
using System.Diagnostics;
using BadukServer;
using Microsoft.AspNetCore.Identity.Data;

[ApiController]
[Authorize]
[Route("[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly ILogger<AuthenticationController> _logger;
    private readonly IUsersService _usersService;
    private readonly IUserRatingService _userRatingService;
    private readonly IUserStatService _userStatService;
    private readonly AuthenticationService _authenticationService;

    [ActivatorUtilitiesConstructorAttribute]
    public AuthenticationController(ILogger<AuthenticationController> logger, IUsersService usersService, IUserRatingService userRatingService, IUserStatService userStatService, AuthenticationService authenticationService)
    {
        _logger = logger;
        _authenticationService = authenticationService;
        _usersService = usersService;
        _userRatingService = userRatingService;
        _userStatService = userStatService;
    }

    [AllowAnonymous]
    [HttpPost("GoogleSignIn")]
    public async Task<ActionResult<UserAuthenticationModel>> GoogleSignIn([FromBody] GoogleSignInTokenBody body)
    {
        var token = body.Token;
        if (token == null)
        {
            return BadRequest("No Google token provided");
        }

        try
        {
            var payload = await _authenticationService.VerifyGoogleTokenId(token);

            var email = payload.Email;

            var user = await _usersService.GetByEmail(email);

            if (user == null)
            {
                return await SignUp(new UserDetailsDto(
                    email: email,
                    username: email,
                    googleSignIn: true,
                    fullName: null,
                    bio: null,
                    avatar: null,
                    nationalilty: null,
                    password: null
                ));
            }
            else
            {
                return await Login(new LoginDto(
                    email: email,
                    password: null,
                    googleToken: body.Token,
                    username: email
                ));
            }
        }
        catch (System.Exception e)
        {
            return BadRequest(e.ToString());
        }
    }

    [AllowAnonymous]
    [HttpPost("PasswordLogIn")]
    public async Task<ActionResult<UserAuthenticationModel>> PasswordLogIn([FromBody] LoginDto userDetails)
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

    [AllowAnonymous]
    [HttpPost("GuestLogin")]
    public async Task<ActionResult<GuestUser>> GuestLogin()
    {
        try
        {
            var newGuest = new GuestUser(ObjectId.GenerateNewId().ToString());
            var token = await _authenticationService.GenerateJSONWebTokenGuestUser(newGuest);
            return Ok(new GuestAuthenticationModel(newGuest, token));
        }
        catch (Exception e)
        {
            return BadRequest(e.ToString());
        }
    }


    private async Task<ActionResult<UserAuthenticationModel>> Login(LoginDto request)
    {
        Debug.Assert(request.Username != null || request.Email != null);
        Debug.Assert(request.GoogleToken != null || request.Password != null);

        User? user = null;
        if (request.Username != null)
        {
            user = await _usersService.GetByUserName(request.Username);
        }
        else
        {
            user = await _usersService.GetByEmail(request.Email!);
        }

        if (user == null)
        {
            _logger.LogInformation("Incorrect username/email provided {email}/{username}", request.Email, request.Username);
            return Unauthorized("User credentials don't match");
        }

        if (request.GoogleToken == null)
        {
            bool validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!validPassword)
            {
                _logger.LogInformation("Incorrect password provided {password}", request.Password);
                return Unauthorized("User credentials don't match");
            }
        }

        _logger.LogInformation("Signup successful {email}", request.Email);
        return Ok(new UserAuthenticationModel(user, await _authenticationService.GenerateJSONWebTokenForNormalUser(user)));
    }

    private async Task<ActionResult<UserAuthenticationModel>> SignUp(UserDetailsDto request)
    {
        var user = await _usersService.GetByUserName(request.Username);

        if (user != null)
        {
            _logger.LogInformation("Incorrect username/email provided {email}/{username}", request.Email, request.Username);
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

        var newUser = await _usersService.CreateUser(request, password);
        var newRatings = await _userRatingService.CreateUserRatings(newUser!.Id!);
        var newStats = await _userStatService.CreateUserStat(newUser!.Id!);

        if (newUser == null)
        {

            _logger.LogInformation("Couldn't create user");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create user");
        }

        _logger.LogInformation("Signup successful {email}", newUser.Email);
        return Ok(new UserAuthenticationModel(newUser, await _authenticationService.GenerateJSONWebTokenForNormalUser(newUser)));
    }


    [HttpGet("GetUser")]
    public async Task<ActionResult<UserAuthenticationModel>> GetUser()
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (userId == null) return Unauthorized();

        var userType = User.FindFirst("user_type")?.Value;
        if (userType == null || userType == "guest_user") return Unauthorized();

        var users = await _usersService.GetByIds([userId]);

        if (users.Count == 0)
        {
            _logger.LogInformation("User doesn't exist");
            return Unauthorized("You don't exist");
        }

        var user = users.First();

        return Ok(new UserAuthenticationModel(user, await _authenticationService.GenerateJSONWebTokenForNormalUser(user)));
    }
}