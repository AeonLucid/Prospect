using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.CloudScript.Models;

namespace Prospect.Server.Api.Services.CloudScript.Functions;

[CloudScriptFunction("GetCharacterVanity")]
public class GetCharacterVanity : ICloudScriptFunction<FYGetCharacterVanityRequest, object?>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetCharacterVanity(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public Task<object?> ExecuteAsync(FYGetCharacterVanityRequest request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return Task.FromResult<object?>(null);
        }
        
        return Task.FromResult<object?>(new
        {
            userId = context.User.FindAuthUserId(),
            error = (object?)null,
            returnVanity = new
            {
                userId = context.User.FindAuthUserId(),
                head_item = new
                {
                    id = "Black02M_Head1",
                    materialIndex = 0,
                },
                boots_item = new
                {
                    id = "StarterOutfit01_Boots_M",
                    materialIndex = 0,
                },
                chest_item = new
                {
                    id = "StarterOutfit01_Chest_M",
                    materialIndex = 0,
                },
                glove_item = new
                {
                    id = "StarterOutfit01_Gloves_M",
                    materialIndex = 0,
                },
                base_suit_item = new
                {
                    id = "StarterOutfit01M_BaseSuit",
                    materialIndex = 0,
                },
                melee_weapon_item = new
                {
                    id = "Melee_Omega",
                    materialIndex = 0,
                },
                body_type = 1,
                archetype_id = "TheProspector",
                slot_index = 0
            }
        });
    }
}