using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Prospect.Server.Api.Models.Client;
using Prospect.Server.Api.Models.Client.Data;

namespace Prospect.Server.Api.Controllers
{
    [Route("Client")]
    [ApiController]
    public class ClientController : Controller
    {
        [HttpPost("LoginWithSteam")]
        [Produces("application/json")]
        public IActionResult LoginWithSteam(ClientLoginWithSteamRequest request)
        {
            var playerIdOne = "AAAABBBBCCCCDDDD"; // PlayFabId
            var playerIdTwo = "0000111122223333"; // EntityId 
            var playerName = "AeonLucid";
            
            var titleId = "A22AB";
            var publisherId = "850902E5B40508ED";
            
            if (!string.IsNullOrEmpty(request.SteamTicket))
            {
                return Ok(new ClientResponse<FServerLoginResult>
                {
                    Code = 200,
                    Status = "OK",
                    Data = new FServerLoginResult
                    {
                        EntityToken = new FEntityTokenResponse
                        {
                            Entity = new FEntityKey
                            {
                                Id = playerIdTwo,
                                Type = "title_player_account",
                                TypeString = "title_player_account"
                            },
                            EntityToken = "RW50aXR5VG9rZW4=",
                            TokenExpiration = DateTime.UtcNow.AddDays(1),
                        },
                        InfoResultPayload = new FGetPlayerCombinedInfoResultPayload
                        {
                            CharacterInventories = new List<object>(),
                            PlayerProfile = new FPlayerProfileModel
                            {
                                DisplayName = playerName,
                                PlayerId = playerIdOne,
                                PublisherId = publisherId,
                                TitleId = titleId
                            },
                            UserDataVersion = 0,
                            UserInventory = new List<object>(),
                            UserReadOnlyDataVersion = 0
                        },
                        LastLoginTime = DateTime.UtcNow,
                        NewlyCreated = false,
                        PlayFabId = playerIdOne,
                        SessionTicket = "SOME",
                        SettingsForUser = new FUserSettings
                        {
                            GatherDeviceInfo = true,
                            GatherFocusInfo = true,
                            NeedsAttribution = false,
                        },
                        TreatmentAssignment = new FTreatmentAssignment
                        {
                            Variables = new List<FVariable>(),
                            Variants = new List<string>()
                        }
                    }
                });
            }
            
            return BadRequest(new ClientResponse
            {
                Code = 400,
                Status = "BadRequest",
                Error = "InvalidSteamTicket",
                ErrorCode = 1010,
                ErrorMessage = "Steam API AuthenticateUserTicket error response .."
            });
        }

        [HttpPost("AddGenericID")]
        [Produces("application/json")]
        public IActionResult AddGenericId(FAddGenericIDRequest request)
        {
            return Ok(new ClientResponse
            {
                Code = 200,
                Status = "OK"
            });
        }

        [HttpPost("UpdateUserTitleDisplayName")]
        [Produces("application/json")]
        public IActionResult AddGenericId(FUpdateUserTitleDisplayNameRequest request)
        {
            return Ok(new ClientResponse<FUpdateUserTitleDisplayNameResult>
            {
                Code = 200,
                Status = "OK",
                Data = new FUpdateUserTitleDisplayNameResult
                {
                    DisplayName = request.DisplayName
                }
            });
        }

        [HttpPost("GetUserData")]
        [Produces("application/json")]
        public IActionResult GetUserData(FGetUserDataRequest request)
        {
            return Ok(new ClientResponse<FGetUserDataResult>
            {
                Code = 200,
                Status = "OK",
                Data = new FGetUserDataResult
                {
                    Data = new Dictionary<string, FUserDataRecord>(),
                    DataVersion = 0
                }
            });
        }

        [HttpPost("GetUserReadOnlyData")]
        [Produces("application/json")]
        public IActionResult GetUserReadOnlyData(FGetUserDataRequest request)
        {
            return Ok(new ClientResponse<FGetUserDataResult>
            {
                Code = 200,
                Status = "OK",
                Data = new FGetUserDataResult
                {
                    Data = new Dictionary<string, FUserDataRecord>(),
                    DataVersion = 0
                }
            });
        }

        [HttpPost("GetUserInventory")]
        [Produces("application/json")]
        public IActionResult GetUserReadOnlyData(FGetUserInventoryRequest request)
        {
            return Ok(new ClientResponse<FGetUserInventoryResult>
            {
                Code = 200,
                Status = "OK",
                Data = new FGetUserInventoryResult
                {
                    Inventory = new List<FItemInstance>
                    {
                        new FItemInstance
                        {
                            ItemId = "Helmet_03",
                            ItemInstanceId = "0102030405060708",
                            ItemClass = "Helmet",
                            PurchaseDate = DateTime.Now.AddDays(-1),
                            CatalogVersion = "StaticItems",
                            DisplayName = "Helmet",
                            UnitPrice = 0,
                            CustomData = new Dictionary<string, string>
                            {
                                ["insurance"] = "None",
                                ["mods"] = "{\"m\":[]}",
                                ["vanity_misc_data"] = "{\"v\":\"\",\"s\":\"\",\"l\":3,\"a\":1,\"d\":300}"
                            }
                        }
                    },
                    VirtualCurrency = new Dictionary<string, int>
                    {
                        ["AE"] = 0,
                        ["AS"] = 0,
                        ["AU"] = 0,
                        ["SC"] = 12345
                    },
                    VirtualCurrencyRechargeTimes = new Dictionary<string, FVirtualCurrencyRechargeTime>()
                }
            });
        }

        [HttpPost("GetTitleData")]
        [Produces("application/json")]
        public IActionResult GetTitleData(FGetTitleDataRequest request)
        {
            return Ok(new ClientResponse<FGetTitleDataResult>
            {
                Code = 200,
                Status = "OK",
                Data = new FGetTitleDataResult()
                {
                    Data = new Dictionary<string, string>()
                }
            });
        }
    }
}