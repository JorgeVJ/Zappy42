#pragma once
#ifdef  _WIN32
#include <winsock2.h>
#endif
#include <string>
#include <vector>
#include <string_view>

std::string Trim(const std::string& str);
bool vector_string_view_add(std::vector<std::string_view> *c, const std::string_view &str) noexcept;
