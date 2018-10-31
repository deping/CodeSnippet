#if defined(_WIN32)
#include <Windows.h>
#include <Shlwapi.h>
#endif

std::string GetApplicationDir()
{
#if defined(_WIN32)
    char fullPath[1024];
    GetModuleFileNameA(NULL, fullPath, _countof(fullPath));
    PathRemoveFileSpecA(fullPath);
    return std::string(fullPath);
#endif
}
