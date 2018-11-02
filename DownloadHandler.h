#pragma once

#include <string>

class DownloadHandler
{
    std::string m_HostNameOrIP;
    int m_Port;
public:
    DownloadHandler(const std::string& downloadDir, const std::string& hostNameOrIP, int port)
        : m_HostNameOrIP(hostNameOrIP)
        , m_Port(port)
    {
    }
    bool Download(const std::string& fileName, const std::string& toFileName);
    bool DownloadByAsio(const std::string& fileName, const std::string& toFileName);
};