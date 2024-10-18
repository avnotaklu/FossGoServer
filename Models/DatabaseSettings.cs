namespace BadukServer.Models;

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
}


public class MongodbCollectionParams<T>
{
    public string Name { get; set; } = null!;
}


// public class DatabaseSettingsFactory
// {
//     private readonly IServiceProvider serviceProvider;

//     public DatabaseSettingsFactory(IServiceProvider serviceProvider)
//     {
//         this.serviceProvider = serviceProvider;
//     }


//     public MongodbCollectionParams<Collection> GetDatabaseSettings<Collection>()
//     {
//         return (MongodbCollectionParams<Collection>)serviceProvider.GetService(typeof(MongodbCollectionParams<Collection>))!;
//     }

// }

// enum Collection
// {
//     Users,
//     Subforums
// }