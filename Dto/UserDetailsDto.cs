namespace BadukServer.Dto
{
    public class UserDetailsDto
    {
        public string Email { get; set; }

        public UserDetailsDto(string email)
        {
            Email = email;
        }
    }
}