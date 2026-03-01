#pragma once
#include <string>

template<typename T>
struct Result
{
	bool Ok;
	std::string Message;
	T Value;

	static Result<T> Success(const T& v)
	{
		return Result<T>{ true, std::string(), v };
	}

	static Result<T> Fail(const std::string& msg)
	{
		return Result<T>{ false, msg, T() };
	}
};