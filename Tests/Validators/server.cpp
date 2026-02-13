#include <iostream>
#include "GetOpt.h"
#include "Validators.h"

namespace Opt {
	namespace Server {
		enum class Id {
			Port,
			Width,
			Height,
			Teams,
			Clients,
			Time,
		};
		constexpr Spec specs[] = {
			{ port_keys, Arity::One, RepeatPolicy::Reject },
			{ width_keys, Arity::One, RepeatPolicy::Reject },
			{ height_keys, Arity::One, RepeatPolicy::Reject },
			{ teams_keys, Arity::OneOrMore, RepeatPolicy::Accumulate },
			{ clients_keys, Arity::One, RepeatPolicy::Reject },
			{ time_keys, Arity::One, RepeatPolicy::Reject },
		};

		constexpr KeyEntry<Server::Id> key_table[] = {
			{ "-p", Opt::Server::Id::Port },
			{ "-x", Opt::Server::Id::Width },
			{ "-y", Opt::Server::Id::Height },
			{ "-n", Opt::Server::Id::Teams },
			{ "-c", Opt::Server::Id::Clients },
			{ "-t", Opt::Server::Id::Time },
		};
		struct Args {
			int port;
			int width;
			int height;
			int time;
			int clients;
			std::vector<std::string_view> *teams;
		};
	}
}

namespace Validators {
	namespace Server {
		struct WidthorHeight {
			static constexpr size_t Min = 17;
			static constexpr size_t Max = 64;
		};
		struct Time {
			static constexpr size_t Min = 1;
			static constexpr size_t Max = 120;
		};
		struct Clients {
			static constexpr size_t Min = 1;
			static constexpr size_t Max_Initial = 16;
			static constexpr size_t Max = 64;
		};
		struct Player {
			static constexpr size_t Max_per_team = 6;
		};
		struct Teams {
			static constexpr size_t NameLenMin = 1;
			static constexpr size_t NameLenMax = 32;
			static constexpr size_t Min = 1;
			static constexpr size_t Max = Clients::Max / Player::Max_per_team;
		};
		Result<size_t> valid_heigth_or_weight(const std::vector<std::string_view> &values, std::vector<std::string_view> *errors)
		{
			if (values.empty())
			{
				vector_string_view_add(errors, Errors::Validation::MissValue);
				return Result<size_t>::Fail(Errors::Validation::MissValue);
			}
			auto r = Utils::parse_int(values[0]);
			if (!r.Ok) {
				vector_string_view_add(errors, r.Message);
				return (Result<size_t>::Fail(r.Message));
			}
			if (!Utils::within_bounds(static_cast<size_t>(r.Value), WidthorHeight::Min, WidthorHeight::Max)) {
				vector_string_view_add(errors, Errors::Validation::Server::InvalidHeightorWidth);
				return (Result<size_t>::Fail(Errors::Validation::Server::InvalidHeightorWidth));
			}
			return (Result<size_t>::Success(static_cast<size_t>(r.Value)));
		}

		bool teams(const std::vector<std::string_view> &values, size_t clientnbr,std::vector<std::string_view> *errors) {
			if (values.empty())
			{
				vector_string_view_add(errors, Errors::Validation::MissValue);
				return (false);
			}
			if (!Utils::within_bounds(values.size(), Teams::Min, Teams::Max))
			{
				vector_string_view_add(errors, Errors::Validation::Server::InvalidTeamNbr);
				return (false);
			}
			for (std::size_t i = 0; i < values.size(); ++i) {
				for (std::size_t j = i + 1; j < values.size(); ++j) {
					if (!Utils::within_bounds(values[i].size(), Teams::NameLenMin, Teams::NameLenMax))
					{
						vector_string_view_add(errors, Errors::Validation::Server::InvalidTeamLen);
						return (false);
					}
					if (!Utils::within_bounds(values[j].size(), Teams::NameLenMin, Teams::NameLenMax))
					{
						vector_string_view_add(errors, Errors::Validation::Server::InvalidTeamLen);
						return (false);
					}
					if (values[i] == values[j]) {
						vector_string_view_add(errors, Errors::Validation::Server::DuplicateTeamName);
						return (false);
					}
				}
			}
			return  (values.size() <= clientnbr);

		}

		Result<size_t> time(const std::vector<std::string_view> &values, std::vector<std::string_view> *errors) {
			if (values.empty())
			{
				vector_string_view_add(errors, Errors::Validation::MissValue);
				return Result<size_t>::Fail(Errors::Validation::MissValue);
			}
			auto r = Utils::parse_int(values[0]);
			if (!r.Ok) {
				vector_string_view_add(errors, r.Message);
				return (Result<size_t>::Fail(r.Message));
			}
			if (!Utils::within_bounds(static_cast<size_t>(r.Value), Time::Min, Time::Max)) {
				vector_string_view_add(errors, Errors::Validation::Server::Time);
				return (Result<size_t>::Fail(Errors::Validation::Server::Time));
			}
			return (Result<size_t>::Success(static_cast<size_t>(r.Value)));
		}

	Result<size_t> clients(const std::vector<std::string_view> &values, std::vector<std::string_view> *errors) {
			if (values.empty())
			{
				vector_string_view_add(errors, Errors::Validation::MissValue);
				return Result<size_t>::Fail(Errors::Validation::MissValue);
			}
			auto r = Utils::parse_int(values[0]);
			if (!r.Ok) {
				vector_string_view_add(errors, r.Message);
				return (Result<size_t>::Fail(r.Message));
			}
			if (!Utils::within_bounds(static_cast<size_t>(r.Value), Clients::Min, Clients::Max_Initial)) {
				vector_string_view_add(errors, Errors::Validation::Server::Clients);
				return (Result<size_t>::Fail(Errors::Validation::Server::Clients));
			}
			return (Result<size_t>::Success(static_cast<size_t>(r.Value)));
		}
	}
}

int main(int argc, char** argv) {
	std::vector<std::string_view> errors;
    Opt::GetOpt<Opt::Server::Id> opts(
									  std::span{Opt::Server::specs},
									  std::span{Opt::Server::key_table});
	Opt::Server::Args args;
    bool ok = opts.parse(argc, argv, &errors);

	if (ok == false)
	{
		std::cerr << "Parsing Error" << std::endl;
		for (auto& e : errors)
			std::cerr << e << std::endl;
		ok = true;
    }
	ok &= validate_arity(opts.values, opts.specs, &errors);
	errors.clear();
	if (ok == true) {
		auto &val = opts.values[static_cast<size_t>(Opt::Server::Id::Port)].values;
		auto port = Validators::port(val, &errors);
		ok &= port.Ok;
		if (ok)
			args.port = port.Value;
	}
	if (ok == true) {
		auto width = Validators::Server::valid_heigth_or_weight(opts.values[static_cast<size_t>(Opt::Server::Id::Width)].values, &errors);
		ok &= width.Ok;
		if (ok)
			args.width = static_cast<int>(width.Value);
	}
	if (ok == true) {
		auto height = Validators::Server::valid_heigth_or_weight(opts.values[static_cast<size_t>(Opt::Server::Id::Height)].values, &errors);
		ok &= height.Ok;
		if (ok)
			args.height = static_cast<int>(height.Value);
	}
	if (ok == true) {
		auto time = Validators::Server::time(opts.values[static_cast<size_t>(Opt::Server::Id::Time)].values, &errors);
		ok &= time.Ok;
		if (ok)
			args.time = static_cast<int>(time.Value);
	}
	if (ok == true) {
		auto clients = Validators::Server::clients(opts.values[static_cast<size_t>(Opt::Server::Id::Clients)].values, &errors);
		ok &= clients.Ok;
		if (ok)
			args.clients = static_cast<int>(clients.Value);
	}
	if (ok == true) {
		ok &= Validators::Server::teams(opts.values[static_cast<size_t>(Opt::Server::Id::Teams)].values, static_cast<size_t>(args.clients), &errors);
		if (ok)
			args.teams = &opts.values[static_cast<size_t>(Opt::Server::Id::Teams)].values;
	}
	if (ok == false)
	{
		std::cerr << "Validation Error" << std::endl;
		for (auto& e : errors)
			std::cerr << e << std::endl;
    }
	std::cout <<  "Teams: " << args.teams << std::endl;
	return (ok == false || !errors.empty());
}
