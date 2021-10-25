#pragma once

#include <cstdio>
#include <Windows.h>

namespace logger
{
	void Attach();

	void Detach();

	bool Print(const char* fmt, ...);

	char ReadKey();
};
