#pragma once

#include <cstdint>
#include <locale>
#include <string>
#include "SDK_Memory.h"

namespace SDK
{
	template<class T>
	struct TArray
	{
		friend struct FString;
		
		TArray()
		{
			Data = nullptr;
			Count = Max = 0;
		}

		int Num() const
		{
			return Count;
		}

		T& operator[](int i)
		{
			return Data[i];
		}

		const T& operator[](int i) const
		{
			return Data[i];
		}

		bool IsValidIndex(int i) const
		{
			return i < Num();
		}

	private:
		T* Data;
		int32_t Count;
		int32_t Max;
	};

	struct FString : private TArray<wchar_t>
	{
		FString(const wchar_t* other)
		{
			const size_t charLen = std::wcslen(other);

			if (charLen == 0)
			{
				Count = 0;
				Max = 0;
				Data = nullptr;
			}
			else
			{
				Count = charLen + 1;
				Max = Count;
				Data = static_cast<wchar_t*>((*GMalloc)->Malloc(Count * 2));

				memset(Data, 0, Count * 2);
				memcpy(Data, other, charLen * 2);
			}
		}

		FString(const std::wstring& other) : FString(other.c_str())
		{
			
		}

		bool IsValid() const
		{
			return Data != nullptr;
		}

		const wchar_t* c_str() const
		{
			return Data;
		}

		std::string ToString() const
		{
			const auto length = std::wcslen(Data);

			std::string str(length, '\0');
			std::use_facet<std::ctype<wchar_t>>(std::locale()).narrow(Data, Data + length, '?', &str[0]);

			return str;
		}
	};

	struct UPlayFabAPISettings
	{
		FString VerticalName;
		FString BaseServiceHost;
		FString TitleId;
		FString AdvertisingIdType;
		FString AdvertisingIdValue;
		bool DisableAdvertising = false;
	};
}
