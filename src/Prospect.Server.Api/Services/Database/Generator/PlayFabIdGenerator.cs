using System;
using System.Security.Cryptography;
using MongoDB.Bson.Serialization;

namespace Prospect.Server.Api.Services.Database.Generator
{
    public class PlayFabIdGenerator : IIdGenerator
    {
        private static readonly RNGCryptoServiceProvider Random = new RNGCryptoServiceProvider();
        
        public object GenerateId(object container, object document)
        {
            Span<byte> data = stackalloc byte[8];
            Random.GetBytes(data);
            return Convert.ToHexString(data);
        }

        public bool IsEmpty(object id)
        {
            return id is not string idStr || string.IsNullOrEmpty(idStr);
        }
    }
}