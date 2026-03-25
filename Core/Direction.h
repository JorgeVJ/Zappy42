#pragma once

enum class Direction {
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest
};

/// <summary>
/// Rotate direction right by 90 degrees
/// </summary>
constexpr Direction RotateRight90(Direction dir)
{
  return (static_cast<Direction>((static_cast<int>(dir) + 2) % 8));
}

/// <summary>
/// Rotate direction left by 90 degrees
/// </summary>
constexpr Direction RotateLeft90(Direction dir)
{
  return (static_cast<Direction>((static_cast<int>(dir) + 6) % 8));
}
