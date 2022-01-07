#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <MinHook.h>
#include <cstdio>
#include "logger.h"
#include "SDK.h"
#include "SDK_Memory.h"

// Pointers are for 211025.1143
// https://steamdb.info/depot/1600361/history/?changeid=M:720055916602660127

/**
 * Variable offset, find with "STOPMOVIECAPTURE", scroll up.
 */
constexpr uintptr_t OffsetGMalloc = 0x5C9D478;

/**
 * Function offset, find with "?sdk=".
 */
constexpr uintptr_t OffsetPlayFabApiGetUrl = 0xCA5010;

/**
 * Function hooks.
 */
typedef SDK::FString(__fastcall* tPlayFabApiGetUrl)(SDK::UPlayFabAPISettings*, const SDK::FString&);

tPlayFabApiGetUrl OldPlayFabApiGetUrl;

SDK::FString __fastcall PlayFabApiGetUrlProxy(SDK::UPlayFabAPISettings* thiz, const SDK::FString& callPath)
{
	logger::Print("[Agent] GetURL called with callPath %s\n", callPath.ToString().c_str());
	
	return std::wstring(L"http://127.0.0.1:5000") + std::wstring(callPath.c_str());
}

DWORD WINAPI OnDllAttach(LPVOID base)
{
	logger::Print("[Agent] DLL Attached\n");

	const auto hModule = GetModuleHandleW(nullptr);
	const auto hModulePtr = reinterpret_cast<uintptr_t>(hModule);

	// Initialize GMalloc.
	SDK::GMalloc = reinterpret_cast<SDK::FMalloc**>(hModulePtr + OffsetGMalloc);

	// Initialize MinHook.
	if (MH_Initialize() != MH_OK)
	{
		logger::Print("[Agent] Failed to initialize MinHook\n");
		FreeLibraryAndExitThread(hModule, 1);
	}

	// Hook UPlayFabAPISettings::GetUrl.
	const auto pTarget = reinterpret_cast<LPVOID>(hModulePtr + OffsetPlayFabApiGetUrl);
	const auto ppOriginal = reinterpret_cast<LPVOID*>(&OldPlayFabApiGetUrl);
	const auto mhStatus = MH_CreateHook(pTarget, &PlayFabApiGetUrlProxy, ppOriginal);

	if (mhStatus != MH_OK)
	{
		logger::Print("[Agent] Failed to create PlayFabAPIGetUrl hook (%d)\n", mhStatus);
		FreeLibraryAndExitThread(hModule, 1);
	}

	if (MH_EnableHook(pTarget) != MH_OK)
	{
		logger::Print("[Agent] Failed to enable PlayFabAPIGetUrl hook\n");
		FreeLibraryAndExitThread(hModule, 1);
	}

	logger::Print("[Agent] DLL Initialized\n");
	ExitThread(1);
}

BOOL WINAPI DllMain(
    const _In_      HINSTANCE hinstDll,
    const _In_      DWORD     fdwReason,
    const _In_opt_  LPVOID    lpvReserved
)
{
	if (fdwReason == DLL_PROCESS_ATTACH)
	{
		DisableThreadLibraryCalls(hinstDll);

		// Allocate a console.
		AllocConsole();
		freopen_s(reinterpret_cast<FILE**>(stdout), "CONOUT$", "w", stdout);

		// Attach logger.
		logger::Attach();
		
		// Spawn thread.
		CreateThread(nullptr, 0, OnDllAttach, hinstDll, 0, nullptr);
	}
	else if (fdwReason == DLL_PROCESS_DETACH)
	{
		// Detach logger.
		logger::Detach();
	}

    return TRUE;
}
