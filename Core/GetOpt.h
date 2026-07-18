#pragma once
#include <stddef.h>
#include <vector>
#include <span>
#include <string>
#include <iostream>
#include <optional>
#include "utils.h"
#include "errors.h"



namespace Opt {
	const std::string Delimiter_Options    = "--";

	const std::string port_keys[]    = { "-p" };
	const std::string width_keys[]   = { "-x" };
	const std::string height_keys[]  = { "-y" };
	const std::string teams_keys[]   = { "-n" };
	const std::string clients_keys[] = { "-c" };
	const std::string time_keys[]    = { "-t" };
	const std::string host_keys[]    = { "-h" };

	enum class Arity : unsigned char {
		Zero,
		ZeroOrOne,
		One,
		OneOrMore,
		Any
    };

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

	constexpr bool arity_stop(Arity a, std::size_t n) noexcept {
		return  (!arity_accepts(a, n + 1));
	}

	constexpr bool arity_required(Arity a) noexcept {
		return  (!arity_accepts(a, 0));
	}

  	struct Value {
		bool present = false;
		std::vector<std::string> values;
	};

	enum class RepeatPolicy {
		Reject,
		Accumulate
	};

	struct Spec {
		std::span<const std::string> keys;
		Arity arity;
		RepeatPolicy repeat;
		bool is_repeatable()  const noexcept;
	};

	template<typename OptId>
	struct KeyEntry {
		std::string key;
		OptId id;
	};



	template<typename OptId>
	class GetOpt {
	public:
		GetOpt(std::span<const Spec> specs,
			   std::span<const KeyEntry<OptId>> keys)
			: specs(specs), keys(keys), values(specs.size()) {}


		bool parse(int argc, char** argv, std::vector<std::string> *errors) {
			bool ok = true;
			bool end_of_options = false;

			for (int i = 1; i < argc; ++i) {
				std::string arg = argv[i];

				if (!end_of_options && arg == Delimiter_Options) {
					return (ok);
				}
				auto id = find(arg);
				if (!id) {
					vector_string_add(errors, Errors::CLI::UnknownOption);
					ok = false;
					continue;
				}

				auto idx = static_cast<size_t>(*id);
				auto& spec = specs[idx];
				auto& val  = values[idx];

				if (val.present && !spec.is_repeatable()) {
					vector_string_add(errors, Errors::CLI::RepeatOption);
					ok = false;
					continue;
				}
				val.present = true;
				if (!consume(spec, val, i, errors, argc, argv)) {
					ok = false;
				}
			}
			return (ok);
		}

		std::span<const Spec> specs;
		std::span<const KeyEntry<OptId>> keys;
		std::vector<Value> values;
	private:
		std::optional<OptId> find(std::string key) const noexcept {
			for (const auto& e : this->keys) {
				if (e.key == key)
					return (e.id);
			}
			return (std::nullopt);
		}


		bool consume(const Spec& spec,
                     Value& val,
                     int& i,
					 std::vector<std::string> *errors,
                     int argc,
                     char** argv)
		{
			bool ok = true;
			std::size_t count = 0;

			while (i + 1 < argc && !arity_stop(spec.arity, count))
			{
				std::string next = argv[i + 1];
				if (!next.empty() && next[0] == '-')
					break;

				try {
					val.values.push_back(next); //throw
					//throwable();
				} catch (const std::exception& e) {
					std::cerr << "Exception STL: " << e.what() << std::endl;
					ok = false;
				} catch (...) {
					std::cerr << "Exception No STL" << std::endl;
					ok = false;
				}
				++i;
				++count;
			}

			if (arity_accepts(spec.arity, count))
				return (ok);
			vector_string_add(errors, Errors::CLI::InvalidArity);
			ok = false;
			return (ok);
		}
	};
}

bool validate_arity(const std::vector<Opt::Value> &values,
			  const std::span<const Opt::Spec> &specs, std::vector<std::string> *errors);
