using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Prospect.Server.Api.Config;
using Prospect.Server.Api.Services.Database.Models;

namespace Prospect.Server.Api.Services.Database
{
    public class DbEntityService : BaseDbService<PlayFabEntity>
    {
        public DbEntityService(IOptions<DatabaseSettings> settings) : base(settings, nameof(DbEntityService))
        {
        }

        public async Task<PlayFabEntity> FindAsync(string userId)
        {
            return await Collection.Find(user => user.UserId == userId).FirstOrDefaultAsync();
        }

        private async Task<PlayFabEntity> CreateAsync(string userId)
        {
            var user = new PlayFabEntity
            {
                UserId = userId
            };
            
            await Collection.InsertOneAsync(user);

            return user;
        }

        public async Task<PlayFabEntity> FindOrCreateAsync(string userId)
        {
            return await FindAsync(userId) ?? 
                   await CreateAsync(userId);
        }
    }
}