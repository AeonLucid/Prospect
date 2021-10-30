#pragma once

#include <cstdint>

#define DEFAULT_ALIGNMENT 0

namespace SDK
{
	class FExec
	{
	public:
		virtual ~FExec() = default;
	private:
		virtual bool Exec(void* world, void* cmd, void* ar) = 0;
	};

	class FMalloc : FExec
	{
	public:
		virtual void* Malloc(SIZE_T Count, uint32_t Alignment = DEFAULT_ALIGNMENT) = 0;

		virtual void* TryMalloc(SIZE_T Count, uint32_t Alignment = DEFAULT_ALIGNMENT) = 0;

		virtual void* Realloc(void* Original, SIZE_T Count, uint32_t Alignment = DEFAULT_ALIGNMENT) = 0;

		virtual void* TryRealloc(void* Original, SIZE_T Count, uint32_t Alignment = DEFAULT_ALIGNMENT) = 0;

		virtual void Free(void* Original) = 0;

		virtual SIZE_T QuantizeSize(SIZE_T Count, uint32_t Alignment)
		{
			return Count;
		}

		virtual bool GetAllocationSize(void* Original, SIZE_T& SizeOut)
		{
			return false;
		}

		virtual void Trim(bool bTrimThreadCaches)
		{
		}

		virtual void SetupTLSCachesOnCurrentThread()
		{
		}

		virtual void ClearAndDisableTLSCachesOnCurrentThread()
		{
		}

		virtual void InitializeStatsMetadata();

		virtual bool Exec(void* InWorld, void* Cmd, void* Ar) override
		{
			return false;
		}

		virtual void UpdateStats() = 0;

		virtual void GetAllocatorStats(void* out_Stats) = 0;

		virtual void DumpAllocatorStats(class FOutputDevice& Ar) = 0;

		virtual bool IsInternallyThreadSafe() const
		{
			return false;
		}

		virtual bool ValidateHeap()
		{
			return(true);
		}

		virtual const TCHAR* GetDescriptiveName() = 0;
	}; 

	FMalloc** GMalloc;
}
