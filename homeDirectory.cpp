
#include <stdlib.h>

std::string userDirectory() {
	size_t returnValue;
	char buffer[256] = "";
	auto err = getenv_s(&returnValue, buffer, "HOME");
    if (err)
	{
		err = getenv_s(&returnValue, buffer, "USERPROFILE");
		return buffer;
    }
    else
	{
		getenv_s(&returnValue, buffer, "HOMEDRIVE");
		auto len = strlen(buffer);
		getenv_s(&returnValue, buffer + len, sizeof(buffer) - len, "HOMEPATH");
		return buffer;
    }
}
