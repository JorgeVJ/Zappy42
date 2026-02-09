#pragma once
#include <string>

//Move to Core
template<typename T>
struct Result
{
	bool Ok;
	std::string_view Message; // Change because string_view don't alocate faster.. c++17
	T Value;

	static Result<T> Success(const T& v) noexcept(noexcept(T(v)))
	{
		return Result<T>{ true, std::string_view(), v };
	}

	static Result<T> Fail(const std::string_view& msg) noexcept
	{
		return Result<T>{ false, msg, T() };
	}
};
