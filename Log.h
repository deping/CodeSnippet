/***************************************************************************
* Copyright (C) 2017, Deping Chen, cdp97531@sina.com
*
* All rights reserved.
* For permission requests, write to the author.
*
* This software is distributed on an "AS IS" basis, WITHOUT WARRANTY OF ANY
* KIND, either express or implied.
***************************************************************************/
#pragma once
#include <boost/log/sources/severity_logger.hpp>

namespace blog
{
// We define our own severity levels
enum class severity_level
{
    info,
    notice,
    warning,
    error,
    fatal
};

void AddLog(severity_level level, const char* file, int line,/* attrs::timer elapsed,*/ const char* msg, ...);

#define LOG_INFO(msg, ...) blog::AddLog(blog::severity_level::info, __FILE__, __LINE__, msg, __VA_ARGS__)
#define LOG_NOTICE(msg, ...) blog::AddLog(blog::severity_level::notice, __FILE__, __LINE__, msg, __VA_ARGS__)
#define LOG_WARN(msg, ...) blog::AddLog(blog::severity_level::warning, __FILE__, __LINE__, msg, __VA_ARGS__)
#define LOG_ERR(msg, ...) blog::AddLog(blog::severity_level::error, __FILE__, __LINE__, msg, __VA_ARGS__)
#define LOG_FATAL(msg, ...) blog::AddLog(blog::severity_level::fatal, __FILE__, __LINE__, msg, __VA_ARGS__)

// InitLog must be called before start of main function.
void InitLog(const char* logFileName);
// FlushLog must be called before end of main function.
void FlushLog();
}