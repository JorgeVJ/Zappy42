#pragma once
#include <iostream>
#include <charconv>
#include <vector>
#include "Result.h"
#include "Errors.h"
#include "utils.h"

namespace Validators {

	struct Port {
		static constexpr int Min        = 1;
		static constexpr int Well_Known = 1023;
		static constexpr int Max        = 65535;
	};
	Result<int> port(const std::vector<std::string_view> &port, std::vector<std::string_view> *errors);

	namespace Utils {
		Result<int> parse_int(std::string_view s) noexcept;
		template<typename T>
		constexpr bool within_bounds(T v, T min, T max) noexcept {
			return v >= min && v <= max;
		}
	}
}
