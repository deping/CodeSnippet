#pragma once
#include <boost/log/sources/severity_logger.hpp>

// We define our own severity levels
enum class severity_level
{
    info,
    notice,
    warning,
    error,
    fatal
};

extern boost::log::sources::severity_logger<severity_level> g_slg;
void AddLog(severity_level level, const char* file, int line,/* attrs::timer elapsed,*/ const char* msg, ...);

#define LOG_INFO(msg, ...) AddLog(severity_level::info, __FILE__, __LINE__, msg, __VA_ARGS__)
#define LOG_NOTICE(msg, ...) AddLog(severity_level::notice, __FILE__, __LINE__, msg, __VA_ARGS__)
#define LOG_WARN(msg, ...) AddLog(severity_level::warning, __FILE__, __LINE__, msg, __VA_ARGS__)
#define LOG_ERR(msg, ...) AddLog(severity_level::error, __FILE__, __LINE__, msg, __VA_ARGS__)
#define LOG_FATAL(msg, ...) AddLog(severity_level::fatal, __FILE__, __LINE__, msg, __VA_ARGS__)

// InitLog must be called before start of main function.
void InitLog();
// FlushLog must be called before end of main function.
void FlushLog();