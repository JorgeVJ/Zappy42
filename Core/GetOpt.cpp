#include "GetOpt.h"

bool Opt::Spec::is_repeatable()  const noexcept
{
	return (this->repeat == RepeatPolicy::Accumulate
			&& (this->arity == Arity::OneOrMore
				|| this->arity == Arity::Any));
}


//Generic validation need to create one with each param
bool validate_arity(const std::vector<Opt::Value> &values,
			  const std::span<const Opt::Spec> &specs, std::vector<std::string_view> *errors)
{
	bool ok = true;

	for (std::size_t i = 0; i < specs.size(); ++i) {
		const auto& spec = specs[i];
		const auto& val  = values[i];

		//Required but miss
		if (arity_required(spec.arity) && !val.present)
		{
			if (errors != nullptr) {
				try {
					errors->push_back(Errors::CLI::MissingOption);  //throw
					//throwable();
				} catch (const std::exception& e) {
					std::cerr << "Exception STL: " << e.what() << std::endl;
				} catch (...) {
					std::cerr << "Exception No STL" << std::endl;
				}
			}
			ok = false;
			continue;
		}
		if (val.present && !arity_accepts(spec.arity, val.values.size())) {
			if (errors != nullptr) {
				try {
					errors->push_back(Errors::CLI::InvalidArity); //throw
					//throwable();
				} catch (const std::exception& e) {
					std::cerr << "Exception STL: " << e.what() << std::endl;
				} catch (...) {
					std::cerr << "Exception No STL" << std::endl;
				}
			}
			ok = false;
			continue;
		}

	}
	return (ok);
}

		// 3. Semantic validation should got another thing for validators
		//			if (val.present && spec.validator) {
		//				ok &= spec.validator(val, errors);
		//			}
