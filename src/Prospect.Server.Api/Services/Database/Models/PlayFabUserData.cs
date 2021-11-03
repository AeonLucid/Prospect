using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Prospect.Server.Api.Services.Database.Models
{
    public class PlayFabUserData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        
        /// <summary>
        ///     PlayFabUser.Id
        /// </summary>
        [BsonRequired]
        [BsonElement("PlayFabId")]
        public string PlayFabId { get; set; }

        [BsonRequired]
        [BsonElement("Key")]
        public string Key { get; set; }
        
        [BsonRequired]
        [BsonElement("Value")]
        public string Value { get; set; }
        
        [BsonRequired]
        [BsonElement("Public")]
        public bool Public { get; set; }
        
        [BsonRequired]
        [BsonElement("LastUpdated")]
        public DateTime LastUpdated { get; set; }
    }
}