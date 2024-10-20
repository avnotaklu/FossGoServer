namespace BadukServer.Dto
{
    public class UserDetailsDto
    {
        public string Email { get; set; }
        public string? Password{ get; set; }
        public bool GoogleSignIn { get; set; }

        public UserDetailsDto(string email,  bool googleSignIn, string? password= null)
        {
            Email = email;
            Password= password;
            GoogleSignIn = googleSignIn;
        }
    }
}