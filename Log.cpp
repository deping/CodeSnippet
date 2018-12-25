/***************************************************************************
* Copyright (C) 2017, Deping Chen, cdp97531@sina.com
*
* All rights reserved.
* For permission requests, write to the author.
*
* This software is distributed on an "AS IS" basis, WITHOUT WARRANTY OF ANY
* KIND, either express or implied.
***************************************************************************/
#include "stdafx.h"
#include <fstream>
#include <iomanip>
#include <iostream>
#include <string>

#include <boost/config.hpp>
#include <boost/core/null_deleter.hpp>
#include <boost/date_time/posix_time/posix_time.hpp>
#include <boost/log/core.hpp>
#include <boost/log/expressions.hpp>
#include <boost/log/expressions/attr_fwd.hpp>
#include <boost/log/expressions/attr.hpp>
#include <boost/log/sinks/async_frontend.hpp>
#include <boost/log/sinks/text_ostream_backend.hpp>
#include <boost/log/sources/logger.hpp>
#include <boost/log/sources/record_ostream.hpp>
#include <boost/log/sources/severity_logger.hpp>
#include <boost/log/attributes/attribute.hpp>
#include <boost/log/attributes/attribute_cast.hpp>
#include <boost/log/attributes/attribute_value.hpp>
#include <boost/log/attributes/mutable_constant.hpp>
#include <boost/log/attributes/timer.hpp>
#include <boost/log/attributes/value_extraction.hpp>
#include <boost/log/utility/setup/common_attributes.hpp>
#include <boost/smart_ptr/shared_ptr.hpp>
#include <boost/thread/shared_mutex.hpp>

#include "Log.h"

namespace blog
{
namespace logging = boost::log;
namespace src = boost::log::sources;
namespace expr = boost::log::expressions;
namespace sinks = boost::log::sinks;
namespace attrs = boost::log::attributes;
namespace keywords = boost::log::keywords;

namespace
{
boost::log::sources::severity_logger<severity_level> g_slg;

// This mutable constant will use shared clocking for reading the value
// and exclusive locking for storing
typedef attrs::mutable_constant<
    int,                                        // attribute value type
    boost::shared_mutex,                        // synchronization primitive
    boost::unique_lock< boost::shared_mutex >,  // exclusive lock type
    boost::shared_lock< boost::shared_mutex >   // shared lock type
> shared_int_att;

typedef attrs::mutable_constant<
    std::string,                                        // attribute value type
    boost::shared_mutex,                        // synchronization primitive
    boost::unique_lock< boost::shared_mutex >,  // exclusive lock type
    boost::shared_lock< boost::shared_mutex >   // shared lock type
> shared_string_att;

shared_string_att g_fileAtt("");
shared_int_att g_lineAtt(0);
//attrs::timer g_timeAtt;


void log_formatter(logging::record_view const& rec, logging::formatting_ostream& strm)
{
    strm << logging::extract<attrs::current_thread_id::value_type>("ThreadID", rec) << " | "
        << logging::extract<severity_level>("Severity", rec) << " | "
        << logging::extract<boost::posix_time::ptime>("TimeStamp", rec) << " | "
        << logging::extract<std::string>("File", rec) << ":"
        << logging::extract<int>("Line", rec) << " | ";
    //auto elapsedTime = logging::extract<int>("ElapsedTime", rec);
    //if (!elapsedTime.empty())
    //{
    //    strm << elapsedTime << " | ";
    //}

    // Finally, put the record message to the stream
    strm << rec[expr::smessage];
}

}

// The operator puts a human-friendly representation of the severity level to the stream
std::ostream& operator<< (std::ostream& strm, severity_level level)
{
    static const char* strings[] =
    {
        "info",
        "notice",
        "warning",
        "error",
        "fatal"
    };

    if (static_cast<std::size_t>(level) < sizeof(strings) / sizeof(*strings))
        strm << strings[(int)level];
    else
        strm << static_cast<int>(level);

    return strm;
}

void AddLog(severity_level level, const char* file, int line,/* attrs::timer elapsed,*/ const char* msg, ...)
{
    char buffer[1024];
    va_list arglist;
    va_start(arglist, msg);
    vsprintf_s(buffer, msg, arglist);
    va_end(arglist);
    g_fileAtt.set(file);
    g_lineAtt.set(line);
    //g_timeAtt.set(elapsed);
    BOOST_LOG_SEV(g_slg, level) << buffer;
}

void InitLog(const char* logFileName)
{
    boost::log::add_common_attributes();
    g_slg.add_attribute("File", g_fileAtt);
    g_slg.add_attribute("Line", g_lineAtt);
    //g_slg.add_attribute("ElapsedTime", g_timeAtt);

    typedef sinks::asynchronous_sink<sinks::text_ostream_backend> text_sink;
    boost::shared_ptr<text_sink> sink = boost::make_shared<text_sink>();

    sink->locked_backend()->add_stream(boost::make_shared< std::ofstream>(logFileName));

    sink->set_formatter(&log_formatter);

    logging::core::get()->add_sink(sink);
}

void FlushLog()
{
    logging::core::get()->flush();
    logging::core::get()->remove_all_sinks();
    g_slg.remove_all_attributes();
}
}