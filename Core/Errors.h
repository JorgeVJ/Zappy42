#pragma once
#include <string_view>

namespace Errors {
	inline constexpr std::string_view Unexpected   = "Unexpected error";

	namespace Exceptions {
		inline constexpr std::string_view VectorPushBack   = "Vector Push Back exceptions";
	}

	namespace CLI {
		inline constexpr std::string_view UnknownOption    = "Unknown option";
		inline constexpr std::string_view RepeatOption     = "Repeated option";
		inline constexpr std::string_view MissingOption    = "Missing option";
		inline constexpr std::string_view TooManyArguments = "Too many arguments";
		inline constexpr std::string_view MissingPort      = "Missing -p <port> argument";
		inline constexpr std::string_view InvalidArity     = "Invalid argument arity";
	}

	namespace Parser {
		inline constexpr std::string_view InvalidToken     = "Invalid token";
		inline constexpr std::string_view UnexpectedEOF    = "Unexpected end of input";
	}

	namespace Validation {

		inline constexpr std::string_view MissValue            = "Missing Value";
		inline constexpr std::string_view InvalidInteger       = "Invalid integer";
		inline constexpr std::string_view InvalidIntegerFormat = "Invalid integer format";
		inline constexpr std::string_view InvalidPort          = "Invalid port number";
		namespace Server {
			inline constexpr std::string_view InvalidHeightorWidth = "Invalid Height or Width number";
			inline constexpr std::string_view DuplicateTeamName    = "Invalid Duplicate Team Name";
			inline constexpr std::string_view Time          = "Invalid Time Nbr";
			inline constexpr std::string_view Clients     = "Invalid Client Nbr";
			inline constexpr std::string_view InvalidTeamNbr     = "Invalid Team Nbr";
			inline constexpr std::string_view InvalidTeamLen     = "Invalid Team Len";


		}
	}
}
