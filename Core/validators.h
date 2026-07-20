#pragma once
#include <iostream>
#include <charconv>
#include <vector>
#include "Result.h"
#include "errors.h"
#include "utils.h"

namespace Validators {

	struct Port {
		static constexpr int Min        = 1;
		static constexpr int Well_Known = 1023;
		static constexpr int Max        = 65535;
	};
  struct TeamNameLen {
    static constexpr size_t Min = 1; // 0; For allow empty string -> "".
    static constexpr size_t Max = 32;
  };
	Result<int> port(const std::vector<std::string> &port, std::vector<std::string> *errors);
  Result<bool> teamname(const std::string &teamname,
                        std::vector<std::string> *errors);

	namespace Utils {
		Result<int> parse_int(std::string s) noexcept;
		template<typename T>
		constexpr bool within_bounds(T v, T min, T max) noexcept {
			return v >= min && v <= max;
		}
	}
}
