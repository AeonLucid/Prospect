using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Prospect.Server.Api.Config;
using Prospect.Server.Api.Services.Database.Models;

namespace Prospect.Server.Api.Services.Database;

public class DbUserDataService : BaseDbService<PlayFabUserData>
{
    public DbUserDataService(IOptions<DatabaseSettings> options) : base(options, nameof(PlayFabUserData))
    {
    }

    public async Task<bool> HasAsync(string playFabId, string key)
    {
        return await Collection.Find(data => data.PlayFabId == playFabId && data.Key == key).AnyAsync();
    }

    public async Task<PlayFabUserData?> FindAsync(string playFabId, string key)
    {
        return await Collection.Find(data => data.PlayFabId == playFabId && data.Key == key).SingleOrDefaultAsync();
    }

    public async Task<PlayFabUserData> InsertAsync(string playFabId, string key, string value, bool isPublic)
    {
        var data = new PlayFabUserData
        {
            PlayFabId = playFabId,
            Key = key,
            Value = value,
            Public = isPublic,
            LastUpdated = DateTime.UtcNow
        };
            
        await Collection.InsertOneAsync(data);

        return data;
    }

    public async Task<IAsyncCursor<PlayFabUserData>> AllAsync(string playFabId, bool publicOnly)
    {
        var query = publicOnly
            ? Collection.Find(data => data.PlayFabId == playFabId && data.Public)
            : Collection.Find(data => data.PlayFabId == playFabId);
            
        return await query.ToCursorAsync();
    }

    public async Task UpdateValueAsync(string dataId, string value)
    {
        var update = Builders<PlayFabUserData>.Update
            .Set(data => data.Value, value)
            .Set(data => data.LastUpdated, DateTime.UtcNow);
            
        await Collection.UpdateOneAsync(data => data.Id == dataId, update);
    }
}