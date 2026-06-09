#pragma once
#include <vector>
#include <string_view>
#include <functional>
#include <memory>
#include "serveroptions.h"
#include "validators.h"

/// <summary>
/// Provides a reusable, modular system for validating and filling Opt::Server::Args.
///
/// This system uses:
/// - Result<T> pattern for all validation results
/// - Chain-of-responsibility pattern for validators
/// - Each argument has a dedicated validator that can be reused and tested independently
/// </summary>
namespace ArgValidation {

	/// <summary>
	/// Base validator/filler for a single argument.
	/// Each argument type (port, width, height, etc.) has its own validator.
	/// </summary>
	class IArgValidator {
	public:
		virtual ~IArgValidator() = default;

		/// <summary>
		/// Validates and fills a single argument in the Args struct
		/// </summary>
		/// <param name="values">Command-line values for this argument</param>
		/// <param name="outArgs">Output struct to fill</param>
		/// <param name="errors">Error messages if validation fails</param>
		/// <returns>Result containing success status and any errors</returns>
		virtual Result<bool> Validate(const std::vector<std::string_view>& values,
									   Opt::Server::Args& outArgs,
									   std::vector<std::string_view>* errors) = 0;

		/// <summary>
		/// Gets human-readable name of this argument
		/// </summary>
		virtual std::string_view GetName() const = 0;
	};

	// ========================================================================
	// PORT VALIDATOR
	// ========================================================================

	class PortValidator : public IArgValidator {
	public:
		Result<bool> Validate(const std::vector<std::string_view>& values,
							  Opt::Server::Args& outArgs,
							  std::vector<std::string_view>* errors) override;

		std::string_view GetName() const override { return "Port"; }
	};

	// ========================================================================
	// WIDTH VALIDATOR
	// ========================================================================

	class WidthValidator : public IArgValidator {
	public:
		Result<bool> Validate(const std::vector<std::string_view>& values,
							  Opt::Server::Args& outArgs,
							  std::vector<std::string_view>* errors) override;

		std::string_view GetName() const override { return "Width"; }
	};

	// ========================================================================
	// HEIGHT VALIDATOR
	// ========================================================================

	class HeightValidator : public IArgValidator {
	public:
		Result<bool> Validate(const std::vector<std::string_view>& values,
							  Opt::Server::Args& outArgs,
							  std::vector<std::string_view>* errors) override;

		std::string_view GetName() const override { return "Height"; }
	};

	// ========================================================================
	// TIME VALIDATOR
	// ========================================================================

	class TimeValidator : public IArgValidator {
	public:
		Result<bool> Validate(const std::vector<std::string_view>& values,
							  Opt::Server::Args& outArgs,
							  std::vector<std::string_view>* errors) override;

		std::string_view GetName() const override { return "Time"; }
	};

	// ========================================================================
	// CLIENTS VALIDATOR
	// ========================================================================

	class ClientsValidator : public IArgValidator {
	public:
		Result<bool> Validate(const std::vector<std::string_view>& values,
							  Opt::Server::Args& outArgs,
							  std::vector<std::string_view>* errors) override;

		std::string_view GetName() const override { return "Clients"; }
	};

	// ========================================================================
	// TEAMS VALIDATOR
	// ========================================================================

	class TeamsValidator : public IArgValidator {
	public:
		Result<bool> Validate(const std::vector<std::string_view>& values,
							  Opt::Server::Args& outArgs,
							  std::vector<std::string_view>* errors) override;

		std::string_view GetName() const override { return "Teams"; }
	};

	// ========================================================================
	// ARGUMENT VALIDATOR CHAIN
	// ========================================================================

	/// <summary>
	/// Validates all arguments in sequence using individual validators.
	/// Provides a clean, reusable interface for validating complete Opt::Server::Args.
	/// </summary>
	class ArgValidatorChain {
	public:
		ArgValidatorChain();

		/// <summary>
		/// Validates and fills all arguments from parsed options
		/// </summary>
		/// <param name="opts">Parsed command-line options</param>
		/// <param name="outArgs">Output - filled arguments struct</param>
		/// <param name="errors">Output - collected error messages</param>
		/// <returns>Result containing success status and collected errors</returns>
		Result<bool> ValidateAll(const Opt::GetOpt<Opt::Server::Id>& opts,
								 Opt::Server::Args& outArgs,
								 std::vector<std::string_view>* errors);

		/// <summary>
		/// Validates a single argument by ID
		/// </summary>
		/// <param name="id">Argument ID to validate</param>
		/// <param name="values">Values for this argument</param>
		/// <param name="outArgs">Output struct to fill</param>
		/// <param name="errors">Error messages if validation fails</param>
		/// <returns>Result containing success status</returns>
		Result<bool> ValidateSingle(Opt::Server::Id id,
									 const std::vector<std::string_view>& values,
									 Opt::Server::Args& outArgs,
									 std::vector<std::string_view>* errors);

		/// <summary>
		/// Registers a custom validator for an argument ID
		/// Allows extending the system with new argument types
		/// </summary>
		/// <param name="id">Argument ID</param>
		/// <param name="validator">Custom validator instance</param>
		void RegisterValidator(Opt::Server::Id id, std::unique_ptr<IArgValidator> validator);

		/// <summary>
		/// Gets a validator by argument ID
		/// </summary>
		/// <param name="id">Argument ID</param>
		/// <returns>Pointer to validator, or nullptr if not found</returns>
		IArgValidator* GetValidator(Opt::Server::Id id) const;

	private:
		std::vector<std::unique_ptr<IArgValidator>> m_validators;

		// Helper method to get validator for an ID
		IArgValidator* GetValidatorForId(Opt::Server::Id id) const;
	};

	// ========================================================================
	// CONVENIENCE FUNCTION
	// ========================================================================

	/// <summary>
	/// Single-call validation of all arguments.
	/// Creates a validator chain, validates all arguments, and returns results.
	/// </summary>
	/// <param name="opts">Parsed command-line options</param>
	/// <param name="outArgs">Output - filled arguments struct</param>
	/// <param name="errors">Output - collected error messages</param>
	/// <returns>Result containing success status and collected errors</returns>
	Result<bool> ValidateServerArgs(const Opt::GetOpt<Opt::Server::Id>& opts,
									 Opt::Server::Args& outArgs,
									 std::vector<std::string_view>* errors);
}
