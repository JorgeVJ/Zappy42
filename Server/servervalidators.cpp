#include "pch.h"
#include "servervalidators.h"
#include "utils.h"

namespace Validators {
	namespace Server {
		Result<size_t> valid_heigth_or_weight(const std::vector<std::string_view> &values,
											   std::vector<std::string_view> *errors)
		{
			if (values.empty())
			{
				vector_string_view_add(errors, Errors::Validation::MissValue);
				return Result<size_t>::Fail(Errors::Validation::MissValue);
			}

			auto r = Utils::parse_int(values[0]);
			if (!r.Ok) {
				vector_string_view_add(errors, r.Message);
				return Result<size_t>::Fail(r.Message);
			}

			if (!Utils::within_bounds(static_cast<size_t>(r.Value),
									   WidthorHeight::Min,
									   WidthorHeight::Max)) {
				vector_string_view_add(errors, Errors::Validation::Server::InvalidHeightorWidth);
				return Result<size_t>::Fail(Errors::Validation::Server::InvalidHeightorWidth);
			}

			return Result<size_t>::Success(static_cast<size_t>(r.Value));
		}

		Result<size_t> time(const std::vector<std::string_view> &values,
						   std::vector<std::string_view> *errors)
		{
			if (values.empty())
			{
				vector_string_view_add(errors, Errors::Validation::MissValue);
				return Result<size_t>::Fail(Errors::Validation::MissValue);
			}

			auto r = Utils::parse_int(values[0]);
			if (!r.Ok) {
				vector_string_view_add(errors, r.Message);
				return Result<size_t>::Fail(r.Message);
			}

			if (!Utils::within_bounds(static_cast<size_t>(r.Value),
									   Time::Min,
									   Time::Max)) {
				vector_string_view_add(errors, Errors::Validation::Server::Time);
				return Result<size_t>::Fail(Errors::Validation::Server::Time);
			}

			return Result<size_t>::Success(static_cast<size_t>(r.Value));
		}

		Result<size_t> clients(const std::vector<std::string_view> &values,
							   std::vector<std::string_view> *errors)
		{
			if (values.empty())
			{
				vector_string_view_add(errors, Errors::Validation::MissValue);
				return Result<size_t>::Fail(Errors::Validation::MissValue);
			}

			auto r = Utils::parse_int(values[0]);
			if (!r.Ok) {
				vector_string_view_add(errors, r.Message);
				return Result<size_t>::Fail(r.Message);
			}

			if (!Utils::within_bounds(static_cast<size_t>(r.Value),
									   Clients::Min,
									   Clients::Max_Initial)) {
				vector_string_view_add(errors, Errors::Validation::Server::Clients);
				return Result<size_t>::Fail(Errors::Validation::Server::Clients);
			}

			return Result<size_t>::Success(static_cast<size_t>(r.Value));
		}

		bool teams(const std::vector<std::string_view> &values,
				   size_t clientnbr,
				   std::vector<std::string_view> *errors)
		{
			// Check if teams vector is empty
			if (values.empty())
			{
				vector_string_view_add(errors, Errors::Validation::MissValue);
				return false;
			}

			// Check if team count is within valid bounds
			if (!Utils::within_bounds(values.size(), Teams::Min, Teams::Max))
			{
				vector_string_view_add(errors, Errors::Validation::Server::InvalidTeamNbr);
				return false;
			}

			// Validate each team name and check for duplicates
			for (std::size_t i = 0; i < values.size(); ++i) {
				// Check team name length
				if (!Utils::within_bounds(values[i].size(),
										  Teams::NameLenMin,
										  Teams::NameLenMax))
				{
					vector_string_view_add(errors, Errors::Validation::Server::InvalidTeamLen);
					return false;
				}

				// Check for duplicate team names
				for (std::size_t j = i + 1; j < values.size(); ++j) {
					if (values[i] == values[j]) {
						vector_string_view_add(errors, Errors::Validation::Server::DuplicateTeamName);
						return false;
					}
				}
			}

			// Check that number of teams doesn't exceed client limit
			return (values.size() <= clientnbr);
		}
	}
}
