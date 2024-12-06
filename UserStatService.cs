public interface IUserStatService
{
    Task<UserStat> GetUserStatAsync(string userId);
    Task<UserStat> SaveUserStat(UserStat userStat);
}