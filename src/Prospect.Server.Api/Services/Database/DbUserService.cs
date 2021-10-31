using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Prospect.Server.Api.Config;
using Prospect.Server.Api.Services.Database.Models;

namespace Prospect.Server.Api.Services.Database
{
    public class DbUserService : BaseDbService<PlayFabUser>
    {
        public DbUserService(IOptions<DatabaseSettings> settings) : base(settings, nameof(PlayFabUser))
        {
        }

        public async Task<PlayFabUser> FindAsync(PlayFabUserAuthType type, string key)
        {
            return await Collection.Find(user => user.Auth.Any(auth => 
                auth.Type == type && 
                auth.Key == key)).FirstOrDefaultAsync();
        }

        private async Task<PlayFabUser> CreateAsync(PlayFabUserAuthType type, string key)
        {
            var user = new PlayFabUser
            {
                DisplayName = "Unknown",
                Auth = new List<PlayFabUserAuth>
                {
                    new PlayFabUserAuth
                    {
                        Type = type,
                        Key = key
                    }
                }
            };
            
            await Collection.InsertOneAsync(user);

            return user;
        }

        public async Task<PlayFabUser> FindOrCreateAsync(PlayFabUserAuthType type, string key)
        {
            return await FindAsync(type, key) ?? 
                   await CreateAsync(type, key);
        }
    }
}