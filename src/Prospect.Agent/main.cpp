#include <cstdio>
#include "main.h"
#include "logger.h"
#include <detours.h>

DWORD WINAPI OnDllAttach(LPVOID base)
{
	logger::Attach();
	logger::Print("[Agent] DLL Attached\n");

	// TODO: Patch.

	logger::Print("[Agent] DLL Exit\n");
	FreeLibraryAndExitThread(static_cast<HMODULE>(base), 1);
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
		
		// Spawn thread.
		CreateThread(nullptr, 0, OnDllAttach, hinstDll, 0, nullptr);
	}

    return TRUE;
}
