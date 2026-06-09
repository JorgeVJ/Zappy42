#pragma once

#include <vector>
#include <string_view>
#include "validators.h"

namespace Validators {
	namespace Server {
		// Size constraints for map dimensions
		struct WidthorHeight {
			static constexpr size_t Min = 17;
			static constexpr size_t Max = 64;
		};

		// Size constraints for time multiplier
		struct Time {
			static constexpr size_t Min = 1;
			static constexpr size_t Max = 120;
		};

		// Size constraints for client limits
		struct Clients {
			static constexpr size_t Min = 1;
			static constexpr size_t Max_Initial = 16;
			static constexpr size_t Max = 64;
		};

		// Size constraints for players per team
		struct Player {
			static constexpr size_t Max_per_team = 6;
		};

		// Size constraints for teams
		struct Teams {
			static constexpr size_t NameLenMin = 1;
			static constexpr size_t NameLenMax = 32;
			static constexpr size_t Min = 1;
			static constexpr size_t Max = Clients::Max_Initial / Player::Max_per_team;
		};

		/// <summary>
		/// Validates map width or height parameter
		/// </summary>
		/// <param name="values">Vector of string values from command line</param>
		/// <param name="errors">Optional error message collection</param>
		/// <returns>Result containing validated size_t value or error</returns>
		Result<size_t> valid_heigth_or_weight(const std::vector<std::string_view> &values, 
											   std::vector<std::string_view> *errors);

		/// <summary>
		/// Validates time multiplier parameter
		/// </summary>
		/// <param name="values">Vector of string values from command line</param>
		/// <param name="errors">Optional error message collection</param>
		/// <returns>Result containing validated size_t value or error</returns>
		Result<size_t> time(const std::vector<std::string_view> &values, 
						   std::vector<std::string_view> *errors);

		/// <summary>
		/// Validates maximum client count parameter
		/// </summary>
		/// <param name="values">Vector of string values from command line</param>
		/// <param name="errors">Optional error message collection</param>
		/// <returns>Result containing validated size_t value or error</returns>
		Result<size_t> clients(const std::vector<std::string_view> &values, 
							   std::vector<std::string_view> *errors);

		/// <summary>
		/// Validates team names parameter
		/// </summary>
		/// <param name="values">Vector of team names from command line</param>
		/// <param name="clientnbr">Maximum number of clients to validate against</param>
		/// <param name="errors">Optional error message collection</param>
		/// <returns>True if all teams are valid, false otherwise</returns>
		Result<bool> teams(const std::vector<std::string_view> &values, 
				   size_t clientnbr,
				   std::vector<std::string_view> *errors);

  }
}
