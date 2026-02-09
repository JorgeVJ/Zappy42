#pragma once
#include <charconv>
#include "Result.h"
#include "Errors.h"
#include "GetOpt.h"

namespace Validators {

  struct Port {
    static constexpr int Min        = 1;
    static constexpr int Well_Known = 1023;
    static constexpr int Max        = 65535;
  };
	bool port(const Opt::Value& opt, ErrorReporter &errors);
	bool unique_strings(const Opt::Value& opt, ErrorReporter& errors);
	bool height(const Opt::Value& opt, ErrorReporter& errors);
  namespace Utils {
    Result<int> parse_int(std::string_view s) noexcept;
    template<typename T>
    constexpr bool within_bounds(T v, T min, T max) noexcept {
      return v >= min && v <= max;
    }

  }
}
