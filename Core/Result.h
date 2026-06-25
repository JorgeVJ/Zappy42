#pragma once
#include <string>
#include <string_view>

template<typename T>
struct Result
{
	bool Ok;
	std::string_view Message;
	T Value;

	static Result<T> Success(const T& v)
	{
		return Result<T>{ true, std::string(), v };
	}

	static Result<T> Fail(std::string_view msg)
	{
		return Result<T>{ false, std::string(msg), T() };
	}
};
