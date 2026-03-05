#include "pch.h"
#include "ArgValidation.h"
#include "servervalidators.h"
#include "utils.h"
#include <algorithm>

namespace ArgValidation {

	// ========================================================================
	// PORT VALIDATOR IMPLEMENTATION
	// ========================================================================

	Result<bool> PortValidator::Validate(const std::vector<std::string_view>& values,
                                       Opt::Server::Args& outArgs,
                                       std::vector<std::string_view>* errors)
	{
		auto port = Validators::port(values, errors);
		if (!port.Ok)
			return Result<bool>::Fail(port.Message);
    outArgs.port = port.Value;
    return Result<bool>::Success(true);
	}

	// ========================================================================
	// WIDTH VALIDATOR IMPLEMENTATION
	// ========================================================================

	Result<bool> WidthValidator::Validate(const std::vector<std::string_view>& values,
                                        Opt::Server::Args& outArgs,
                                        std::vector<std::string_view>* errors)
	{
		auto width = Validators::Server::valid_heigth_or_weight(values, errors);
		if (!width.Ok)
			return Result<bool>::Fail(width.Message);
    outArgs.width = static_cast<int>(width.Value);
    return Result<bool>::Success(true);
	}

	// ========================================================================
	// HEIGHT VALIDATOR IMPLEMENTATION
	// ========================================================================

	Result<bool> HeightValidator::Validate(const std::vector<std::string_view>& values,
                                         Opt::Server::Args& outArgs,
                                         std::vector<std::string_view>* errors)
	{
		auto height = Validators::Server::valid_heigth_or_weight(values, errors);
		if (!height.Ok)
      return Result<bool>::Fail(height.Message);
    outArgs.height = static_cast<int>(height.Value);
    return Result<bool>::Success(true);
	}

	// ========================================================================
	// TIME VALIDATOR IMPLEMENTATION
	// ========================================================================

	Result<bool> TimeValidator::Validate(const std::vector<std::string_view>& values,
                                       Opt::Server::Args& outArgs,
                                       std::vector<std::string_view>* errors)
	{
		auto time = Validators::Server::time(values, errors);
		if (!time.Ok)
      return Result<bool>::Fail(time.Message);

    outArgs.time = static_cast<int>(time.Value);
    return Result<bool>::Success(true);

	}

	// ========================================================================
	// CLIENTS VALIDATOR IMPLEMENTATION
	// ========================================================================

	Result<bool> ClientsValidator::Validate(const std::vector<std::string_view>& values,
                                          Opt::Server::Args& outArgs,
                                          std::vector<std::string_view>* errors)
	{
		auto clients = Validators::Server::clients(values, errors);
		if (!clients.Ok)
      return Result<bool>::Fail(clients.Message);
    outArgs.clients = static_cast<int>(clients.Value);
    return Result<bool>::Success(true);

	}

	// ========================================================================
	// TEAMS VALIDATOR IMPLEMENTATION
	// ========================================================================

	Result<bool> TeamsValidator::Validate(const std::vector<std::string_view>& values,
										 Opt::Server::Args& outArgs,
										 std::vector<std::string_view>* errors)
	{
		// Note: We need to validate against client count, which should already be filled
    auto teams = Validators::Server::teams(values, static_cast<size_t>(outArgs.clients), errors);
		if (!teams.Ok)
      return (teams);

    outArgs.teams = values;
    return Result<bool>::Success(true);
	}

	// ========================================================================
	// ARGUMENT VALIDATOR CHAIN IMPLEMENTATION
	// ========================================================================

	ArgValidatorChain::ArgValidatorChain()
	{
		// Register default validators
		m_validators.resize(6);

		m_validators[static_cast<size_t>(Opt::Server::Id::Port)] =
			std::make_unique<PortValidator>();
		m_validators[static_cast<size_t>(Opt::Server::Id::Width)] =
			std::make_unique<WidthValidator>();
		m_validators[static_cast<size_t>(Opt::Server::Id::Height)] =
			std::make_unique<HeightValidator>();
		m_validators[static_cast<size_t>(Opt::Server::Id::Teams)] =
			std::make_unique<TeamsValidator>();
		m_validators[static_cast<size_t>(Opt::Server::Id::Clients)] =
			std::make_unique<ClientsValidator>();
		m_validators[static_cast<size_t>(Opt::Server::Id::Time)] =
			std::make_unique<TimeValidator>();
	}

	Result<bool> ArgValidatorChain::ValidateAll(const Opt::GetOpt<Opt::Server::Id>& opts,
												Opt::Server::Args& outArgs,
												std::vector<std::string_view> *errors)
	{
		bool allSuccess = true;

		// Order matters: validate clients before teams (teams validation depends on client count)
		// Validate in this order: Port -> Width -> Height -> Time -> Clients -> Teams
		std::vector<Opt::Server::Id> validationOrder = {
			Opt::Server::Id::Port,
			Opt::Server::Id::Width,
			Opt::Server::Id::Height,
			Opt::Server::Id::Time,
			Opt::Server::Id::Clients,
			Opt::Server::Id::Teams,
		};

		for (Opt::Server::Id id : validationOrder)
		{
			size_t idIndex = static_cast<size_t>(id);
			const auto& values = opts.values[idIndex].values;

			Result<bool> result = ValidateSingle(id, values, outArgs, errors);

			if (!result.Ok)
			{
				allSuccess = false;
        if (errors)
          vector_string_view_add(errors, result.Message);
			}
    }
		if (allSuccess)
			return Result<bool>::Success(true);
		else
			return Result<bool>::Fail("One or more argument validation errors");
	}

	Result<bool> ArgValidatorChain::ValidateSingle(Opt::Server::Id id,
													const std::vector<std::string_view>& values,
													Opt::Server::Args& outArgs,
													std::vector<std::string_view>* errors)
	{
		IArgValidator* validator = GetValidatorForId(id);
		if (!validator)
		{
			if (errors)
				vector_string_view_add(errors, "No validator registered for this argument");
			return Result<bool>::Fail("No validator registered");
		}

		return validator->Validate(values, outArgs, errors);
	}

	void ArgValidatorChain::RegisterValidator(Opt::Server::Id id,
											  std::unique_ptr<IArgValidator> validator)
	{
		size_t idIndex = static_cast<size_t>(id);
		if (idIndex >= m_validators.size())
			m_validators.resize(idIndex + 1);

		m_validators[idIndex] = std::move(validator);
	}

	IArgValidator* ArgValidatorChain::GetValidator(Opt::Server::Id id) const
	{
		return GetValidatorForId(id);
	}

	IArgValidator* ArgValidatorChain::GetValidatorForId(Opt::Server::Id id) const
	{
		size_t idIndex = static_cast<size_t>(id);
		if (idIndex >= m_validators.size())
			return nullptr;

		return m_validators[idIndex].get();
	}

	// ========================================================================
	// CONVENIENCE FUNCTION
	// ========================================================================

	Result<bool> ValidateServerArgs(const Opt::GetOpt<Opt::Server::Id>& opts,
									 Opt::Server::Args& outArgs,
									 std::vector<std::string_view>* errors)
	{
		ArgValidatorChain chain;
		return chain.ValidateAll(opts, outArgs, errors);
	}
}
