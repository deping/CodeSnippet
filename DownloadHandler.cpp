#include "stdafx.h"

#include <windows.h>
#include <Wininet.h>
#pragma comment(lib, "Wininet.lib")

#include <cassert>
#include <fstream>
#include <ostream>
#include <string>

#include <boost/algorithm/string.hpp>

#include "DownloadHandler.h"
#include "Log.h"
#include "Utility.h"

using namespace std;

bool DownloadHandler::Download(const std::string& fromFileName, const std::string& toFileName)
{
    std::string fileName(fromFileName);
    boost::replace_all(fileName, "\\", "/");
    if(fileName.compare(0, 2, "./") == 0)
        fileName = fileName.substr(2);

    HINTERNET hIntSession = ::InternetOpenA("UpdateApp", INTERNET_OPEN_TYPE_DIRECT, NULL, NULL, 0);
    ON_EXIT([hIntSession]() { ::InternetCloseHandle(hIntSession); });

    HINTERNET hHttpSession =
        InternetConnectA(hIntSession, m_HostNameOrIP.c_str(), m_Port, 0, 0, INTERNET_SERVICE_HTTP, 0, NULL);
    ON_EXIT([hHttpSession]() { ::InternetCloseHandle(hHttpSession); });

    HINTERNET hHttpRequest = HttpOpenRequestA(
        hHttpSession,
        "GET",
        fileName.c_str(),
        0, 0, 0, INTERNET_FLAG_RELOAD, 0);
    ON_EXIT([hHttpRequest]() { ::InternetCloseHandle(hHttpRequest); });

    if (!HttpSendRequestA(hHttpRequest, nullptr, 0, nullptr, 0)) {
        DWORD dwErr = GetLastError();
        return false;
    }

    // base::binary is a must, or "\r\n" will becom "\r\r\n".
    ofstream of(toFileName, ios_base::out | ios_base::binary);
    if (!of)
    {
        return false;
    }
    char szBuffer[1024];
    DWORD dwRead = 0;
    BOOL success = FALSE;
    while ((success = ::InternetReadFile(hHttpRequest, szBuffer, sizeof(szBuffer) - 1, &dwRead)) && dwRead) {
        szBuffer[dwRead] = 0;
        of.write(szBuffer, dwRead);
        dwRead = 0;
    }

    return !!success;
}
