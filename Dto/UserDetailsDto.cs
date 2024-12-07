namespace BadukServer.Dto
{
    public class UserDetailsDto
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public bool GoogleSignIn { get; set; }
        public string Username { get; set; }
        public string? FullName { get; set; }
        public string? Bio { get; set; }
        public string? Avatar { get; set; }
        public string? Nationalilty { get; set; }


        public UserDetailsDto(string? email, string? password, bool googleSignIn, string username, string? fullName, string? bio, string? avatar, string? nationalilty)
        {
            Email = email;
            Password = password;
            GoogleSignIn = googleSignIn;
            Username = username;
            FullName = fullName;
            Bio = bio;
            Avatar = avatar;
            Nationalilty = nationalilty;
        }
    }
}