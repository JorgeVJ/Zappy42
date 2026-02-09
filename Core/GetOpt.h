#pragma once
#include <stddef.h>
#include <vector>
#include <span>
#include <string>
#include <iostream>
#include <optional>
#include "Errors.h"

struct Error {
    size_t pos;
    std::string_view msg;
};


class ErrorReporter {
 public:
	void add(size_t pos, std::string_view msg);
	bool has_errors() const noexcept;
	const std::vector<Error>& all() const noexcept;
	void clean() noexcept ;
 private:
  std::vector<Error> errors_;
};

namespace Opt {
	constexpr std::string_view Delimiter_Options    = "--";

	constexpr std::string_view port_keys[]    = { "-p" };
	constexpr std::string_view width_keys[]   = { "-x" };
	constexpr std::string_view height_keys[]  = { "-y" };
	constexpr std::string_view teams_keys[]   = { "-n" };
	constexpr std::string_view clients_keys[] = { "-c" };
	constexpr std::string_view time_keys[]    = { "-t" };
	constexpr std::string_view host_keys[]    = { "-h" };

	enum class Arity : unsigned char {
		Zero,
		ZeroOrOne,
		One,
		OneOrMore,
		Any
    };

	constexpr bool arity_stop(Arity a, std::size_t n) noexcept {
		switch (a)
		{
		case Arity::Zero:
			return (true);
		case Arity::ZeroOrOne:
		case Arity::One:
			return (n >= 1);
		case Arity::OneOrMore:
		case Arity::Any:
			return (false);
		}
		return (true);
	}

	constexpr bool arity_required(Arity a) noexcept {
		switch (a)
		{
		case Arity::Zero:
		case Arity::ZeroOrOne:
		case Arity::Any:
			return (false);
		default:
			break;
		}
		return (true);
	}

	constexpr bool arity_accepts(Arity a, std::size_t n) noexcept {
		switch (a) {
		case Arity::Zero:
			return (n == 0);
		case Arity::ZeroOrOne:
			return (n == 0 || n == 1);
		case Arity::One:
			return n == 1;
		case Arity::OneOrMore:
			return n >= 1;
		case Arity::Any:
			return true;
		}
		return false;
	}

  	struct Value {
		bool present = false;
		std::vector<std::string_view> values;

		bool is_present() const noexcept;

		bool has_values() const noexcept;

		bool missing() const noexcept;
	};

	enum class RepeatPolicy {
		Reject,
		Accumulate
	};

	struct Spec {
		std::span<const std::string_view> keys;
		Arity arity;
		//bool (*validator)(const Value&, ErrorReporter &errors);
		RepeatPolicy repeat;
		bool is_repeatable()  const noexcept;
	};

	bool validate(const std::vector<Value> &values,
				  const std::span<const Spec> &specs, ErrorReporter &errors) {
		bool ok = true;

		for (std::size_t i = 0; i < specs.size(); ++i) {
			const auto& spec = specs[i];
			const auto& val  = values[i];

			//Required but miss
			if (arity_required(spec.arity) && !val.present)
			{
				errors.add(0, Errors::CLI::MissingOption);  //throw
				ok = false;
				continue;
			}

			if (val.present && !arity_accepts(spec.arity, val.values.size())) {
				errors.add(0, Errors::CLI::InvalidArity); //throw
				ok = false;
				continue;
			}
			// 3. Semantic validation should got another thing for validators
			//			if (val.present && spec.validator) {
			//				ok &= spec.validator(val, errors);
			//			}
		}
		return (ok);
	}


	template<typename OptId>
	struct KeyEntry {
		std::string_view key;
		OptId id;
	};



	template<typename OptId>
	class GetOpt {
	public:
		GetOpt(std::span<const Spec> specs,
			   std::span<const KeyEntry<OptId>> keys,
			   ErrorReporter &errors)
			: specs_(specs), keys_(keys), errors_(errors) {}


		bool parse(int argc, char** argv) {
			bool ok = true;
			bool end_of_options = false;

			for (int i = 1; i < argc; ++i) {
				std::string_view arg = argv[i];

				if (!end_of_options && arg == Delimiter_Options) {
					end_of_options = true;
					continue;
				}

				if (end_of_options) {
					this->positionals_.push_back(arg);
					continue;
				}
				auto id = find(arg);
				if (!id) {
					errors_.add(i, Errors::CLI::UnknownOption);
					ok = false;
					continue;
				}

				auto idx = static_cast<size_t>(*id);
				auto& spec = specs_[idx];
				auto& val  = values_[idx];

				if (val.present && !spec.is_repeatable()) {
					errors_.add(i, Errors::CLI::RepeatOption);
					ok = false;
					continue;
				}
				val.present = true;
				if (!consume(spec, val, i, argc, argv)) {
					ok = false;
				}
			}
			return (ok);
		}

		bool validate() {
			bool ok = true;

			for (std::size_t i = 0; i < specs_.size(); ++i) {
				const auto& spec = specs_[i];
				const auto& val  = values_[i];

				//Required but miss
				if (arity_required(spec.arity) && !val.present)
				{
					this->errors_.add(i, Errors::CLI::MissingOption);  //throw
					ok = false;
					continue;
				}

				if (val.present && !arity_accepts(spec.arity, val.values.size())) {
					this->errors_.add(i, Errors::CLI::InvalidArity); //throw
					ok = false;
					continue;
				}
				// 3. Semantic validation
				// if (val.present && spec.validator) {
				// 	ok &= spec.validator(val, errors_);
				// }
			}
			return (ok);
		}


		const std::vector<Value>& getValues() const noexcept {
			return (this->values_);
		}

		const std::span<const Spec>& getSpecs() const noexcept {
			return (this->specs_);
		}

	private:
		std::optional<OptId> find(std::string_view key) const noexcept {
			for (const auto& e : this->keys_) {
				if (e.key == key)
					return (e.id);
			}
			return (std::nullopt);
		}


		bool consume(const Spec& spec,
                     Value& val,
                     int& i,
                     int argc,
                     char** argv)
		{
			std::size_t count = 0;

			while (i + 1 < argc && !arity_stop(spec.arity, count))
			{
				std::string_view next = argv[i + 1];
				if (!next.empty() && next[0] == '-')
					break;

				val.values.push_back(next); //throw
				++i;
				++count;
			}

			if (arity_accepts(spec.arity, count))
				return (true);
			errors_.add(i, Errors::CLI::InvalidArity);
			return (false);
		}

		std::span<const Spec> specs_;
		std::span<const KeyEntry<OptId>> keys_;
		ErrorReporter& errors_;

		std::vector<Value> values_{specs_.size()};
		std::vector<std::string_view> positionals_{specs_.size()};
	};
}
