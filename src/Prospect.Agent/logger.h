#pragma once

namespace logger
{
	void Attach();

	void Detach();

	bool Print(const char* fmt, ...);

	char ReadKey();
}