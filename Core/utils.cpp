#include "pch.h"
#include <iostream>
#include "utils.h"

std::string Trim(const std::string& str)
{
    size_t start = str.find_first_not_of(" \t\r\n");
    size_t end = str.find_last_not_of(" \t\r\n");
    if (start == std::string::npos) return "";
    return str.substr(start, end - start + 1);
}

bool vector_string_view_add(std::vector<std::string_view> *c, const std::string_view &str) noexcept {
	if (c != nullptr) {
		try {
			c->push_back(str);
		} catch (const std::exception& e) {
			std::cerr << "Exception STL: " << e.what() << std::endl;
			return (false);
		} catch (...) {
			std::cerr << "Exception No STL" << std::endl;
			return (false);
		}
	}
	return (true);
}
