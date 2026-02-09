#include "GetOpt.h"


void ErrorReporter::add(size_t pos,  std::string_view  msg)
{
        this->errors_.push_back({pos, msg});
}

bool ErrorReporter::has_errors() const noexcept {
    return (!this->errors_.empty());
}

const std::vector<Error>& ErrorReporter::all() const noexcept {
    return this->errors_;
  }

void ErrorReporter::clean() noexcept {
	this->errors_.clear();
}

bool Opt::Spec::is_repeatable()  const noexcept
{
	return (this->repeat == RepeatPolicy::Accumulate
			&& (this->arity == Arity::OneOrMore
				|| this->arity == Arity::Any));
}
std::ostream &operator<<(std::ostream &os, const Error &e)
{
	return (os << "Pos argument: " << e.pos << " Msg: " << e.msg);
}

bool Opt::Value::is_present() const noexcept
{
	return (this->present);
}

bool Opt::Value::has_values() const noexcept {
	return !values.empty();
}

bool Opt::Value::missing() const noexcept {
	return !present;
}
