using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Prospect.Server.Api.Config;

namespace Prospect.Server.Api.Services.Database;

public abstract class BaseDbService<T>
{
    protected BaseDbService(IOptions<DatabaseSettings> options, string collection)
    {
        var settings = options.Value;
        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);

        Collection = database.GetCollection<T>(collection);
    }
        
    protected IMongoCollection<T> Collection { get; }
}