using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Prospect.Server.Api.Config;
using Prospect.Server.Api.Models.Client;
using Prospect.Server.Api.Models.Client.Data;
using Prospect.Server.Api.Services.Auth;
using Prospect.Server.Api.Services.Auth.Extensions;
using Prospect.Server.Api.Services.Auth.User;
using Prospect.Server.Api.Services.Database;
using Prospect.Server.Api.Services.Database.Models;
using Prospect.Server.Api.Services.UserData;
using Prospect.Server.Steam;

namespace Prospect.Server.Api.Controllers
{
    [ApiController]
    [Route("Client")]
    public class ClientController : Controller
    {
        private readonly PlayFabSettings _settings;
        private readonly AuthTokenService _authTokenService;
        private readonly DbUserService _userService;
        private readonly DbEntityService _entityService;
        private readonly UserDataService _userDataService;
        private readonly TitleDataService _titleDataService;

        public ClientController(IOptions<PlayFabSettings> settings, 
            AuthTokenService authTokenService, 
            DbUserService userService, 
            DbEntityService entityService,
            UserDataService userDataService,
            TitleDataService titleDataService)
        {
            _settings = settings.Value;
            _authTokenService = authTokenService;
            _userService = userService;
            _entityService = entityService;
            _userDataService = userDataService;
            _titleDataService = titleDataService;
        }
        
        [AllowAnonymous]
        [HttpPost("LoginWithSteam")]
        [Produces("application/json")]
        public async Task<IActionResult> LoginWithSteam(ClientLoginWithSteamRequest request)
        {
            if (!string.IsNullOrEmpty(request.SteamTicket))
            {
                var ticket = AppTicket.Parse(request.SteamTicket);
                if (ticket.IsValid && ticket.HasValidSignature)
                {
                    var userSteamId = ticket.SteamId.ToString();
                    
                    var user = await _userService.FindOrCreateAsync(PlayFabUserAuthType.Steam, userSteamId);
                    var entity = await _entityService.FindOrCreateAsync(user.Id);
                    
                    var userTicket = _authTokenService.GenerateUser(entity);
                    var entityTicket = _authTokenService.GenerateEntity(entity);

                    await _userDataService.InitAsync(user.Id);
                    
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
                                    Id = entity.Id,
                                    Type = "title_player_account",
                                    TypeString = "title_player_account"
                                },
                                EntityToken = entityTicket,
                                TokenExpiration = DateTime.UtcNow.AddDays(6), // TODO:
                            },
                            InfoResultPayload = new FGetPlayerCombinedInfoResultPayload
                            {
                                CharacterInventories = new List<object>(),
                                PlayerProfile = new FPlayerProfileModel
                                {
                                    DisplayName = user.DisplayName,
                                    PlayerId = user.Id,
                                    PublisherId = _settings.PublisherId,
                                    TitleId = _settings.TitleId
                                },
                                UserDataVersion = 0,
                                UserInventory = new List<object>(),
                                UserReadOnlyDataVersion = 0
                            },
                            LastLoginTime = DateTime.UtcNow, // TODO:
                            NewlyCreated = false, // TODO:
                            PlayFabId = user.Id,
                            SessionTicket = userTicket,
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
        [Authorize(AuthenticationSchemes = UserAuthenticationOptions.DefaultScheme)]
        public IActionResult AddGenericId(FAddGenericIDRequest request)
        {
            return Ok(new ClientResponse<object>
            {
                Code = 200,
                Status = "OK",
                Data = new object()
            });
        }

        [HttpPost("UpdateUserTitleDisplayName")]
        [Produces("application/json")]
        [Authorize(AuthenticationSchemes = UserAuthenticationOptions.DefaultScheme)]
        public IActionResult UpdateUserTitleDisplayName(FUpdateUserTitleDisplayNameRequest request)
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

        [HttpPost("UpdateUserData")]
        [Produces("application/json")]
        [Authorize(AuthenticationSchemes = UserAuthenticationOptions.DefaultScheme)]
        public async Task<IActionResult> UpdateUserData(FUpdateUserDataRequest request)
        {
            var userId = User.FindAuthUserId();
            await _userDataService.UpdateAsync(userId, request.PlayFabId, request.Data);
            
            return Ok(new ClientResponse<FUpdateUserDataResult>
            {
                Code = 200,
                Status = "OK",
                Data = new FUpdateUserDataResult
                {
                    DataVersion = 0
                }
            });
        }

        [HttpPost("GetUserData")]
        [Produces("application/json")]
        [Authorize(AuthenticationSchemes = UserAuthenticationOptions.DefaultScheme)]
        public async Task<IActionResult> GetUserData(FGetUserDataRequest request)
        {
            var userId = User.FindAuthUserId();
            var userData = await _userDataService.FindAsync(userId, request.PlayFabId, request.Keys);
            
            return Ok(new ClientResponse<FGetUserDataResult>
            {
                Code = 200,
                Status = "OK",
                Data = new FGetUserDataResult
                {
                    Data = userData,
                    DataVersion = 0
                }
            });
        }

        [HttpPost("GetUserReadOnlyData")]
        [Produces("application/json")]
        [Authorize(AuthenticationSchemes = UserAuthenticationOptions.DefaultScheme)]
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
        [Authorize(AuthenticationSchemes = UserAuthenticationOptions.DefaultScheme)]
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
                        // new FItemInstance
                        // {
                        //     ItemId = "Helmet_03",
                        //     ItemInstanceId = "0102030405060708",
                        //     ItemClass = "Helmet",
                        //     PurchaseDate = DateTime.Now.AddDays(-1),
                        //     CatalogVersion = "StaticItems",
                        //     DisplayName = "Helmet",
                        //     UnitPrice = 0,
                        //     CustomData = new Dictionary<string, string>
                        //     {
                        //         ["insurance"] = "None",
                        //         ["mods"] = "{\"m\":[]}",
                        //         ["vanity_misc_data"] = "{\"v\":\"\",\"s\":\"\",\"l\":3,\"a\":1,\"d\":300}"
                        //     }
                        // }
                    },
                    VirtualCurrency = new Dictionary<string, int>
                    {
                        ["AE"] = 0,
                        ["AS"] = 0,
                        ["AU"] = int.MaxValue,
                        ["SC"] = int.MaxValue
                    },
                    VirtualCurrencyRechargeTimes = new Dictionary<string, FVirtualCurrencyRechargeTime>()
                }
            });
        }

        [HttpPost("GetTitleData")]
        [Produces("application/json")]
        [Authorize(AuthenticationSchemes = UserAuthenticationOptions.DefaultScheme)]
        public IActionResult GetTitleData(FGetTitleDataRequest request)
        {
            return Ok(new ClientResponse<FGetTitleDataResult>
            {
                Code = 200,
                Status = "OK",
                Data = new FGetTitleDataResult()
                {
                    Data = _titleDataService.Find(request.Keys)
                }
            });
        }
    }
}