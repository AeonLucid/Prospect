using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Prospect.Server.Api.Services.Database.Generator;

namespace Prospect.Server.Api.Services.Database.Models
{
    /// <summary>
    ///     master_player_account
    /// </summary>
    public class PlayFabUser
    {
        [BsonId(IdGenerator = typeof(PlayFabIdGenerator))]
        public string Id { get; set; }
        
        [BsonRequired]
        [BsonElement("DisplayName")]
        public string DisplayName { get; set; }
        
        [BsonRequired]
        [BsonElement("Auth")]
        public List<PlayFabUserAuth> Auth { get; set; }
    }
}