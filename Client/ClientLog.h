#pragma once
#include <string>
#include <ostream>
#include <iostream>
#include <sstream>

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#endif

enum class ClientLogChannel
{
	Client,
	Server,
	Action,
	Player,
	Explorer,
	Feeder,
	Chaman,
	Stoner,
	Debug,
	Warning,
	Error
};

namespace ClientLog
{
	inline const char* Prefix(ClientLogChannel channel)
	{
		switch (channel)
		{
		case ClientLogChannel::Client:   return "[Client] ";
		case ClientLogChannel::Server:   return "[Server] ";
		case ClientLogChannel::Action:   return "[Action] ";
		case ClientLogChannel::Player:   return "[Player] ";
		case ClientLogChannel::Explorer: return "[Explorer] ";
		case ClientLogChannel::Feeder:   return "[Feeder] ";
		case ClientLogChannel::Chaman:   return "[Chaman] ";
		case ClientLogChannel::Stoner:   return "[Stoner] ";
		case ClientLogChannel::Debug:    return "[Debug] ";
		case ClientLogChannel::Warning:  return "[Warning] ";
		case ClientLogChannel::Error:    return "[Error] ";
		default:                         return "";
		}
	}

	inline WORD ColorFor(ClientLogChannel channel)
	{
		switch (channel)
		{
		case ClientLogChannel::Client:   return FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE | FOREGROUND_INTENSITY;
		case ClientLogChannel::Server:   return FOREGROUND_GREEN | FOREGROUND_BLUE | FOREGROUND_INTENSITY;
		case ClientLogChannel::Action:   return FOREGROUND_GREEN | FOREGROUND_INTENSITY;
		case ClientLogChannel::Player:   return FOREGROUND_GREEN | FOREGROUND_BLUE | FOREGROUND_INTENSITY;
		case ClientLogChannel::Explorer: return FOREGROUND_BLUE | FOREGROUND_INTENSITY;
		case ClientLogChannel::Feeder:   return FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_INTENSITY;
		case ClientLogChannel::Chaman:   return FOREGROUND_RED | FOREGROUND_BLUE | FOREGROUND_INTENSITY;
		case ClientLogChannel::Stoner:   return FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE;
		case ClientLogChannel::Debug:    return FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_INTENSITY;
		case ClientLogChannel::Warning:  return FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_INTENSITY;
		case ClientLogChannel::Error:    return FOREGROUND_RED | FOREGROUND_INTENSITY;
		default:                         return FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE;
		}
	}

	inline void Write(ClientLogChannel channel, const std::string& message, std::ostream& stream = std::cout)
	{
#ifdef _WIN32
		HANDLE h = GetStdHandle(stream.rdbuf() == std::cout.rdbuf() ? STD_OUTPUT_HANDLE : STD_ERROR_HANDLE);
		CONSOLE_SCREEN_BUFFER_INFO info{};
		const bool canColor = (h != INVALID_HANDLE_VALUE) && GetConsoleScreenBufferInfo(h, &info);
		if (canColor)
			SetConsoleTextAttribute(h, ColorFor(channel));
#endif
		stream << Prefix(channel) << message << std::endl;
		stream.flush();
#ifdef _WIN32
		if (h != INVALID_HANDLE_VALUE)
			SetConsoleTextAttribute(h, info.wAttributes);
#endif
	}
}

#define LOG_CLIENT(expr) do { std::ostringstream _oss; _oss << expr; ClientLog::Write(ClientLogChannel::Client, _oss.str()); } while (0)
#define LOG_SERVER(expr) do { std::ostringstream _oss; _oss << expr; ClientLog::Write(ClientLogChannel::Server, _oss.str()); } while (0)
#define LOG_ACTION(expr) do { std::ostringstream _oss; _oss << expr; ClientLog::Write(ClientLogChannel::Action, _oss.str()); } while (0)
#define LOG_PLAYER(expr) do { std::ostringstream _oss; _oss << expr; ClientLog::Write(ClientLogChannel::Player, _oss.str()); } while (0)
#define LOG_EXPLORER(expr) do { std::ostringstream _oss; _oss << expr; ClientLog::Write(ClientLogChannel::Explorer, _oss.str()); } while (0)
#define LOG_FEEDER(expr) do { std::ostringstream _oss; _oss << expr; ClientLog::Write(ClientLogChannel::Feeder, _oss.str()); } while (0)
#define LOG_CHAMAN(expr) do { std::ostringstream _oss; _oss << expr; ClientLog::Write(ClientLogChannel::Chaman, _oss.str()); } while (0)
#define LOG_STONER(expr) do { std::ostringstream _oss; _oss << expr; ClientLog::Write(ClientLogChannel::Stoner, _oss.str()); } while (0)
#define LOG_DEBUG(expr) do { std::ostringstream _oss; _oss << expr; ClientLog::Write(ClientLogChannel::Debug, _oss.str()); } while (0)
#define LOG_WARNING(expr) do { std::ostringstream _oss; _oss << expr; ClientLog::Write(ClientLogChannel::Warning, _oss.str(), std::cerr); } while (0)
#define LOG_ERROR(expr) do { std::ostringstream _oss; _oss << expr; ClientLog::Write(ClientLogChannel::Error, _oss.str(), std::cerr); } while (0)
