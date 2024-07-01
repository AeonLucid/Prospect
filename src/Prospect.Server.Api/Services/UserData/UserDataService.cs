﻿using Prospect.Server.Api.Models.Client.Data;
using Prospect.Server.Api.Services.Database;

namespace Prospect.Server.Api.Services.UserData;

public class UserDataService
{
    private readonly ILogger<UserDataService> _logger;
    private readonly DbUserDataService _dbUserDataService;

    public UserDataService(ILogger<UserDataService> logger, DbUserDataService dbUserDataService)
    {
        _logger = logger;
        _dbUserDataService = dbUserDataService;
    }

    /// <summary>
    ///     Initialize data for the given PlayFabId.
    /// </summary>
    public async Task InitAsync(string playFabId)
    {
        // TODO: Proper objects.
        var defaultData = new Dictionary<string, (bool isPublic, string value)>
        {
            ["Generators__2021_09_09"] = (true, "[{\"generatorId\":\"playerquarters_gen_aurum\",\"lastClaimTime\":{\"seconds\":0}},{\"generatorId\":\"playerquarters_gen_kmarks\",\"lastClaimTime\":{\"seconds\":0}},{\"generatorId\":\"playerquarters_gen_crate\",\"lastClaimTime\":{\"seconds\":0}}]"),
            ["InventoryInfo"] = (true, "{\"inventoryStashLimit\":75,\"inventoryBagLimit\":300,\"inventorySafeLimit\":5}"),
            ["LOADOUT"] = (true, $"{{\"id\":\"\",\"userId\":\"{playFabId}\",\"kit\":\"\",\"shield\":\"\",\"helmet\":\"\",\"weaponOne\":\"\",\"weaponTwo\":\"\",\"bag\":null,\"bagItemsAsJsonStr\":\"\",\"safeItemsAsJsonStr\":\"\"}}"),
            ["OnboardingProgression"] = (true, "{\"currentMissionID\":\"TalkToBadum\",\"progress\":0,\"showPopup\":true}"),
            ["PickaxeUpgradeLevel"] = (true, "0"),
            ["PlayerQuartersLevel"] = (true, "{\"level\":1,\"upgradeStartedTime\":{\"seconds\":0}}"),
        };

        foreach (var (key, (isPublic, value)) in defaultData)
        {
            if (!await _dbUserDataService.HasAsync(playFabId, key))
            {
                await _dbUserDataService.InsertAsync(playFabId, key, value, isPublic);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="currentUserId">
    ///     The authenticated user id.
    /// </param>
    /// <param name="requestUserId">
    ///     The requested user id.
    /// </param>
    /// <param name="keys"></param>
    /// <returns></returns>
    public async Task<Dictionary<string, FUserDataRecord>> FindAsync(
        string currentUserId, 
        string requestUserId, 
        List<string> keys)
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            throw new ArgumentNullException(nameof(currentUserId));
        }
            
        if (requestUserId == null)
        {
            requestUserId = currentUserId;
        }

        var other = currentUserId != requestUserId;
        var result = new Dictionary<string, FUserDataRecord>();
            
        if (keys != null && keys.Count > 0)
        {
            foreach (var key in keys)
            {
                var data = await _dbUserDataService.FindAsync(requestUserId, key);
                if (data == null)
                {
                    // TODO: Error?
                    continue;
                }

                if (!data.Public && other)
                {
                    // TODO: Error?
                    continue;
                }
                    
                result.Add(data.Key, new FUserDataRecord
                {
                    LastUpdated = data.LastUpdated,
                    Permission = data.Public ? UserDataPermission.Public : UserDataPermission.Private,
                    Value = data.Value
                });
            }   
        }
        else
        {
            var cursor = await _dbUserDataService.AllAsync(requestUserId, other);

            while (await cursor.MoveNextAsync())
            {
                foreach (var data in cursor.Current)
                {
                    result.Add(data.Key, new FUserDataRecord
                    {
                        LastUpdated = data.LastUpdated,
                        Permission = data.Public ? UserDataPermission.Public : UserDataPermission.Private,
                        Value = data.Value
                    });
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="currentUserId">
    ///     The authenticated user id.
    /// </param>
    /// <param name="targetUserId">
    ///     The target user id.
    /// </param>
    /// <param name="changes"></param>
    /// <returns></returns>
    public async Task UpdateAsync(
        string currentUserId, 
        string? targetUserId, 
        Dictionary<string, string> changes)
    {
        if (changes.Count == 0)
        {
            return;
        }
            
        if (string.IsNullOrEmpty(currentUserId))
        {
            throw new ArgumentNullException(nameof(currentUserId));
        }
            
        if (targetUserId == null)
        {
            targetUserId = currentUserId;
        }

        // Whether we are updating someone else.
        var other = currentUserId != targetUserId;
            
        foreach (var (key, value) in changes)
        {
            var data = await _dbUserDataService.FindAsync(currentUserId, key);
            if (data == null)
            {
                if (other)
                {
                    _logger.LogWarning("User {PlayFabId} attempted to update non-existing key {Key} of another user {PlayFabIdOther}", currentUserId, key, targetUserId);
                }
                else
                {
                    _logger.LogDebug("User {PlayFabId} created key {Key}", currentUserId, key);
                    await _dbUserDataService.InsertAsync(currentUserId, key, value, false);
                }
                    
                continue;
            }
                
            if (!data.Public && other)
            {
                _logger.LogWarning("User {PlayFabId} attempted to update non-public key {Key} of another user {PlayFabIdOther}", currentUserId, key, data.PlayFabId);
                continue;
            }
                
            await _dbUserDataService.UpdateValueAsync(data.Id, value);
        }
    }
}