using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Prospect.Server.Api.Models.Client;
using Prospect.Server.Api.Models.CloudScript;
using Prospect.Server.Api.Models.CloudScript.Data;
using Prospect.Server.Api.Services.Auth.Entity;
using Prospect.Server.Api.Services.Auth.Extensions;

namespace Prospect.Server.Api.Controllers
{
    [Route("CloudScript")]
    [ApiController]
    [Authorize(AuthenticationSchemes = EntityAuthenticationOptions.DefaultScheme)]
    public class CloudScriptController : Controller
    {
        private readonly ILogger<CloudScriptController> _logger;

        public CloudScriptController(ILogger<CloudScriptController> logger)
        {
            _logger = logger;
        }
        
        [HttpPost("ExecuteFunction")]
        [Produces("application/json")]
        public IActionResult ExecuteFunction(FExecuteFunctionRequest request)
        {
            _logger.LogInformation("Executing function {Function}", request.FunctionName);

            object result = null;

            switch (request.FunctionName)
            {
                case "RequestMaintenanceModeState":
                    result = new
                    {
                        enabled = false
                    };
                    break;
                case "GetCharacterVanity":
                    result = new
                    {
                        userId = User.FindAuthUserId(),
                        error = (object) null,
                        returnVanity = new
                        {
                            userId = User.FindAuthUserId(),
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
                    };
                    break;
                case "SetMatchAllowJoin":
                    result = new
                    {
                        sessionId = (object)null,
                        success = false
                    };
                    break;
                case "GetFriendList":
                    // TODO: ?
                    result = new
                    {

                    };
                    break;
                case "GetPlayerSets":
                    result = new
                    {
                        success = true,
                        entries = new []
                        {
                            new
                            {
                                setData = new
                                {
                                    id = "",
                                    userId = User.FindAuthUserId(),
                                    kit = "",
                                    shield = "",
                                    helmet = "",
                                    weaponOne = "",
                                    weaponTwo = "",
                                    bag = "",
                                    bagItemsAsJsonStr = "",
                                    safeItemsAsJsonStr = ""
                                },
                                items = Array.Empty<object>()
                            }
                        }
                    };
                    break;
                case "RequestFactionProgression":
                    result = new
                    {
                        factions = new []
                        {
                            new
                            {
                                factionId = "ICA",
                                currentProgression = 0
                            },
                            new
                            {
                                factionId = "Korolev",
                                currentProgression = 0
                            },
                            new
                            {
                                factionId = "Osiris",
                                currentProgression = 0
                            },
                        },
                        userId = User.FindAuthUserId(),
                        error = ""
                    };
                    break;
                case "GetPlayersInventoriesLimits":
                    result = new
                    {
                        success = true,
                        entries = new []
                        {
                            new {
                                userId = User.FindAuthUserId(),
                                inventoryStashLimit = 75,
                                inventoryBagLimit = 300,
                                inventorySafeLimit = 5
                            }
                        }
                    };
                    break;
                case "UpdateRetentionBonus":
                    result = new
                    {
                        playerData = new
                        {
                            daysClaimed = 0,
                            lastClaimTime = new
                            {
                                seconds = 1635033600
                            }
                        },
                        userId = User.FindAuthUserId(),
                        error = (object)null
                    };
                    break;
                case "GetCraftingInProgressData":
                    result = new FYGetCraftingInProgressDataResult
                    {
                        UserId = User.FindAuthUserId(),
                        Error = null,
                        ItemCurrentlyBeingCrafted = new FYItemCurrentlyBeingCrafted
                        {
                            ItemId = null,
                            ItemRarity = -1,
                            PurchaseAmount = -1,
                            UtcTimestampWhenCraftingStarted = new FYTimestamp
                            {
                                Seconds = 0
                            }
                        }
                    };
                    break;
                case "RequestPlayerContracts":
                    result = new FYGetPlayerContractsResult
                    {
                        UserId = User.FindAuthUserId(),
                        Error = null,
                        ActiveContracts = new List<FYActiveContractPlayerData>(),
                        FactionsContracts = new FYFactionsContractsData
                        {
                            Boards = new List<FYFactionContractsData>
                            {
                                new FYFactionContractsData
                                {
                                    FactionId = "ICA",
                                    Contracts = new List<FYFactionContractData>
                                    {
                                        new FYFactionContractData
                                        {
                                            ContractId = "NEW-Easy-ICA-Gather-1",
                                            ContractIsLockedDueToLowFactionReputation = false
                                        },
                                        new FYFactionContractData
                                        {
                                            ContractId = "NEW-Medium-ICA-Uplink-1",
                                            ContractIsLockedDueToLowFactionReputation = false
                                        },
                                        new FYFactionContractData
                                        {
                                            ContractId = "NEW-Hard-ICA-Uplink-1",
                                            ContractIsLockedDueToLowFactionReputation = false
                                        }
                                    }
                                },
                                new FYFactionContractsData
                                {
                                    FactionId = "Korolev",
                                    Contracts = new List<FYFactionContractData>
                                    {
                                        new FYFactionContractData
                                        {
                                            ContractId = "NEW-Easy-KOR-Mine-4",
                                            ContractIsLockedDueToLowFactionReputation = false
                                        },
                                        new FYFactionContractData
                                        {
                                            ContractId = "NEW-Medium-KOR-Mine-1",
                                            ContractIsLockedDueToLowFactionReputation = false
                                        },
                                        new FYFactionContractData
                                        {
                                            ContractId = "NEW-Hard-KOR-PvP-6",
                                            ContractIsLockedDueToLowFactionReputation = false
                                        }
                                    }
                                },
                                new FYFactionContractsData
                                {
                                    FactionId = "Osiris",
                                    Contracts = new List<FYFactionContractData>
                                    {
                                        new FYFactionContractData
                                        {
                                            ContractId = "NEW-Easy-Osiris-Brightcaps-1",
                                            ContractIsLockedDueToLowFactionReputation = false
                                        },
                                        new FYFactionContractData
                                        {
                                            ContractId = "NEW-Medium-Osiris-Gather-1",
                                            ContractIsLockedDueToLowFactionReputation = false
                                        },
                                        new FYFactionContractData
                                        {
                                            ContractId = "NEW-Hard-Osiris-Gather-7",
                                            ContractIsLockedDueToLowFactionReputation = false
                                        }
                                    }
                                }
                            },
                            LastBoardRefreshTimeUtc = new FYTimestamp
                            {
                                Seconds = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                            }
                        },
                        RefreshHours24UtcFromBackend = 12
                    };
                    break;
                default:
                    _logger.LogWarning("Missing function {Function}", request.FunctionName);
                    
                    result = new
                    {

                    };
                    break;
            }
            
            return Ok(new ClientResponse<FExecuteFunctionResult>
            {
                Code = 200,
                Status = "OK",
                Data = new FExecuteFunctionResult
                {
                    ExecutionTimeMilliseconds = 12,
                    FunctionName = request.FunctionName,
                    FunctionResult = result
                }
            });
        }
    }
}