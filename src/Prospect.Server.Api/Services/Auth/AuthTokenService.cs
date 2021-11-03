using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Prospect.Server.Api.Services.Database.Models;

namespace Prospect.Server.Api.Services.Auth
{
    public class AuthTokenService
    {
        private const string DefaultIssuer = "ProspectApi";
        private const string DefaultAudience = "Prospect";
        
        private readonly SymmetricSecurityKey _securityKey;
        private readonly JwtSecurityTokenHandler _tokenHandler;
        
        public AuthTokenService(IOptions<AuthTokenSettings> options)
        {
            _securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(options.Value.Secret));
            _tokenHandler = new JwtSecurityTokenHandler();
        }

        private string CreateToken(IEnumerable<Claim> claims)
        {
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = DefaultIssuer,
                Audience = DefaultAudience,
                SigningCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            return _tokenHandler.WriteToken(token);
        }

        public string GenerateUser(PlayFabEntity user)
        {
            return CreateToken(new[]
            {
                new Claim(AuthClaimTypes.UserId, user.UserId),
                new Claim(AuthClaimTypes.EntityId, user.Id),
                new Claim(AuthClaimTypes.Type, AuthType.User),
            });
        }

        public string GenerateEntity(PlayFabEntity entity)
        {
            return CreateToken(new[]
            {
                new Claim(AuthClaimTypes.EntityId, entity.Id),
                new Claim(AuthClaimTypes.Type, AuthType.Entity),
            });
        }

        public ClaimsPrincipal Validate(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            try
            {
                return tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = DefaultIssuer,
                    ValidAudience = DefaultAudience,
                    IssuerSigningKey = _securityKey
                }, out _);
            }
            catch
            {
                return null;
            }
        }
    }
}