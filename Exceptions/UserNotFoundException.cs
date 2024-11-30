public class UserNotFoundException : Exception
{
    public UserNotFoundException(string userId) : base($"User with id {userId} not found")
    {
    }
}