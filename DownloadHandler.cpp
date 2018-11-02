#include "stdafx.h"

#include <boost/asio.hpp>

#include <windows.h>
#include <Wininet.h>
#pragma comment(lib, "Wininet.lib")

#include <cassert>
#include <fstream>
#include <ostream>
#include <string>
#include <sstream>

#include <boost/algorithm/string.hpp>
#include <boost/asio/buffer.hpp>

#include "DownloadHandler.h"
#include "Log.h"
#include "Utility.h"

using namespace std;
using namespace boost;
using boost::asio::ip::tcp;

bool DownloadHandler::Download(const std::string& fromFileName, const std::string& toFileName)
{
    std::string fileName(fromFileName);
    boost::replace_all(fileName, "\\", "/");

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

bool DownloadHandler::DownloadByAsio(const std::string & fromFileName, const std::string & toFileName)
{
    std::string fileName(fromFileName);
    boost::replace_all(fileName, "\\", "/");
    // There must be a / before fileName for http request.
    if (!fileName.empty() && fileName[0] != '/')
    {
        fileName.insert(0, 1, '/');
    }
    try
    {
        boost::asio::io_service io_service;

        // Get a list of endpoints corresponding to the server name.
        tcp::resolver resolver(io_service);
        char port[12];
        tcp::resolver::query query(m_HostNameOrIP, itoa(m_Port, port, 10));
        tcp::resolver::iterator endpoint_iterator = resolver.resolve(query);

        // Try each endpoint until we successfully establish a connection.
        tcp::socket socket(io_service);
        boost::asio::connect(socket, endpoint_iterator);

        // Form the request. We specify the "Connection: close" header so that the
        // server will close the socket after transmitting the response. This will
        // allow us to treat all data up until the EOF as the content.
        std::stringstream request;
        request << "GET " << fileName << " HTTP/1.1\r\n";
        request << "Host: " << m_HostNameOrIP << ":" << m_Port << "\r\n";
        // DONT ADD: Accept: */*, or it will fail with eof exception.
        //request << "Accept: */*\r\n";
        request << "Content-Length: 0" << "\r\n";
        request << "Connection: close\r\n\r\n";

        // Send the request.
        //std::vector<boost::asio::const_buffer> buffers;
        //buffers.push_back(request);
        boost::asio::write(socket, asio::buffer(request.str()));
        string reqStr(request.str());
        boost::replace_all(reqStr, "\r", "");
        
        LOG_INFO(reqStr.c_str());

        // Read the response status line. The response streambuf will automatically
        // grow to accommodate the entire line. The growth may be limited by passing
        // a maximum size to the streambuf constructor.
        boost::asio::streambuf response;
        boost::asio::read_until(socket, response, "\r\n");

        // Check that response is OK.
        std::istream response_stream(&response);
        std::string http_version;
        response_stream >> http_version;
        unsigned int status_code;
        response_stream >> status_code;
        std::string status_message;
        std::getline(response_stream, status_message);
        // pop '\r'
        status_message.pop_back();
        std::ostringstream response_str;
        response_str << http_version << ' ' << status_code << ' ' << status_message;
        if (!response_stream || http_version.substr(0, 5) != "HTTP/")
        {
            LOG_ERR(response_str.str().c_str());
            LOG_ERR("Invalid response");
            return false;
        }
        if (status_code != 200)
        {
            LOG_ERR(response_str.str().c_str());
            return false;
        }

        // Read the response headers, which are terminated by a blank line.
        boost::asio::read_until(socket, response, "\r\n\r\n");

        // Process the response headers.
        std::string header;
        while (std::getline(response_stream, header) && header != "\r")
        {
            // pop '\r'
            header.pop_back();
            response_str << '\n' << header;
        }
        LOG_INFO(response_str.str().c_str());

        // base::binary is a must, or "\r\n" will becom "\r\r\n".
        ofstream of(toFileName, ios_base::out | ios_base::binary);
        if (!of)
        {
            return false;
        }
        // Write whatever content we already have to output.
        if (response.size() > 0)
            of << &response;

        // Read until EOF, writing data to output as we go.
        boost::system::error_code error;
        while (boost::asio::read(socket, response,
            boost::asio::transfer_at_least(1), error))
            of << &response;
        if (error != boost::asio::error::eof)
            throw boost::system::system_error(error);
    }
    catch (std::exception& e)
    {
        //std::cout << "Exception: " << e.what() << "\n";
        LOG_ERR("Exception: %s", e.what());
        return false;
    }
    return true;
}
