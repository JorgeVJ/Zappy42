#include "Validators.h"

Result<int> Validators::Utils::parse_int(std::string_view s)  noexcept
{
  int out = 0;
  auto [ptr, ec] = std::from_chars(s.data(), s.data() + s.size(), out, 10);

  if (ec != std::errc{})
    return Result<int>::Fail(Errors::Validation::InvalidInteger);
  if (ptr != s.data() + s.size())
    return Result<int>::Fail(Errors::Validation::InvalidIntegerFormat);
  return Result<int>::Success(out);
}

bool Validators::port(const Opt::Value& opt, ErrorReporter &errors)
{
  Result<int> port = Validators::Utils::parse_int(opt.values[0]);
  if (port.Ok)
  {
    if (Validators::Utils::within_bounds<int>(port.Value,
                                              Validators::Port::Min,
                                              Validators::Port::Max))
      return (true);
    else
    {
      errors.add(0, Errors::Validation::InvalidPort);
      return (false);
    }
  }
  errors.add(0, port.Message);
  return (false);
}

bool Validators::height(const Opt::Value& opt, ErrorReporter& errors) {
  auto r = Utils::parse_int(opt.values[0]);
  if (!r.Ok) {
    errors.add(0, r.Message);
    return (false);
  }
  if (r.Value <= 1) {
    errors.add(0, Errors::Validation::InvalidHeight);
    return (false);
  }
  return (true);
}

bool Validators::unique_strings(const Opt::Value& opt, ErrorReporter& errors) {
  for (std::size_t i = 0; i < opt.values.size(); ++i) {
    for (std::size_t j = i + 1; j < opt.values.size(); ++j) {
      if (opt.values[i] == opt.values[j]) {
        errors.add(0, Errors::Validation::DuplicateTeamName);
        return (false);
      }
    }
  }
  return (true);
}
