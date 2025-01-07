using BadukServer;

public class UpdateProfileResult
{
    public User User { get; set; }

    public UpdateProfileResult(User user)
    {
        User = user;
    }
}