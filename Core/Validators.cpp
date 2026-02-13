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

Result<int> Validators::port(const std::vector<std::string_view> &values, std::vector<std::string_view> *errors)
{
	if (values.empty())
		return Result<int>::Fail(Errors::Validation::MissValue);
	Result<int> port = Validators::Utils::parse_int(values[0]);
	if (port.Ok)
	{
		if (Validators::Utils::within_bounds<int>(port.Value,
												  Validators::Port::Min,
												  Validators::Port::Max))
			return (port);
		else
		{
			vector_string_view_add(errors, Errors::Validation::InvalidPort);
			return (Result<int>::Fail(Errors::Validation::InvalidPort));
		}
	}
	vector_string_view_add(errors, port.Message);
	return (port);
}
