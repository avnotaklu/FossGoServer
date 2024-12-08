using MongoDB.Driver;

public interface IMongoOperationLogger
{
    public Task<T> Operation<T>(Func<Task<T>> func);
}

// REVIEW: For now this handles all exceptions
public class MongoOperationLogger : IMongoOperationLogger
{
    private readonly ILogger<MongoOperationLogger> _logger;

    public MongoOperationLogger(ILogger<MongoOperationLogger> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Wraps an operation and logs any exceptions
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="func"></param>
    /// <returns></returns>
    /// <exception cref="DatabaseOperationException"></exception>
    /// <exception cref="OperationException"></exception>
    public async Task<T> Operation<T>(Func<Task<T>> func)
    {
        try
        {
            var res = await func();
            _logger.LogInformation("Mongo Operation Successful, [{T}] Result: {res}", typeof(T), res);
            return res;
        }
        catch (MongoException e)
        {
            _logger.LogError(e, "Error in Mongo Operation");
            throw new DatabaseOperationException(e.Message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in Operation");
            throw new OperationException(e.Message);
        }
    }
}

public class OperationException : Exception
{
    public OperationException(string message) : base("Operation Error: " + message)
    {
    }
}

public class DatabaseOperationException : OperationException
{
    public DatabaseOperationException(string message) : base("Database Error: " + message)
    {
    }
}