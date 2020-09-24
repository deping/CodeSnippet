// MFCConsole.cpp : 此文件包含 "main" 函数。程序执行将在此处开始并结束。
//

#include "pch.h"
#include <iostream>
#include <fstream>
// 在VS2017, VS2019中必须开启/std:c++17。工程属性>C++>语言
#include<filesystem>
namespace fs = std::filesystem;

enum SHX_TYPE { REGFONT, UNIFONT, BIGFONT, SHAPEFILE, UNKNOWN };

SHX_TYPE getShxType(const char* file)
{
	std::ifstream fs(file);
	char buffer[32] = { 0 };
	fs.read(buffer, 32);
	fs.close();
	SHX_TYPE type;
	if (_strnicmp(&buffer[11], "unifont", 7) == 0)
	{
		type = UNIFONT;
	}
	else if (_strnicmp((const char*)&buffer[11], "bigfont", 7) == 0)
	{
		type = BIGFONT;
	}
	else if (_strnicmp((const char*)&buffer[11], "shapes", 6) == 0)
	{
		unsigned short* m_pIndice = (unsigned short*)&buffer[30];
		if (*m_pIndice == 0)
		{
			type = REGFONT;
		}
		else
		{
			type = SHAPEFILE;
		}
	}
	else
	{
		type = UNKNOWN;
	}
	return type;
}

const char* getType(SHX_TYPE type)
{
#define STRINGIFY_TYPE(type)	\
	case type:					\
		return #type
	switch (type)
	{
		STRINGIFY_TYPE(REGFONT);
		STRINGIFY_TYPE(UNIFONT);
		STRINGIFY_TYPE(BIGFONT);
		STRINGIFY_TYPE(SHAPEFILE);
		STRINGIFY_TYPE(UNKNOWN);
	default:
		return "";
	}
#undef STRINGIFY_TYPE
}

int main()
{
	const char* path = "C:\\Program Files (x86)\\AutoCAD 2007\\Fonts\\";
	fs::path fspath(path);
	if (!fs::exists(fspath))
		return 1;
	fs::directory_entry entry(fspath);
	if (entry.is_directory())
	{
		fs::directory_iterator list(fspath);
		for (auto& p : list) {
			if (p.is_regular_file())
			{
				auto ext = p.path().extension().string();
				if (_stricmp(ext.c_str(), ".shx") == 0)
				{
					SHX_TYPE type = getShxType(p.path().string().c_str());
					std::cout << getType(type) << p.path().filename() << std::endl;
				}
			}
		}
	}
}
