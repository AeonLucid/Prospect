﻿namespace Prospect.Unreal.Net.Actors;

public class AGameModeBase : AInfo
{
    public void PreLogin(string options, string address, FUniqueNetIdRepl uniqueId, out string? errorMessage)
    {
        // Login unique id must match server expected unique id type OR No unique id could mean game doesn't use them
        errorMessage = null;
    }
}