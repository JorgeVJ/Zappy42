#pragma once
#include <string>

/// <summary>
/// Saves the Broadcasts from other Players.
/// </summary>
struct MessageEntry
{
	/// <summary>
	/// Contains level information.
	/// </summary>
	std::string Message = "";

	/// <summary>
	/// True if the Player must wait for the other players to come.
	/// </summary>
	bool MarcoPolo;

	/// <summary>
	/// Indicates the Tile number direction from the message origin.
	/// </summary>
	int From;

	/// <summary>
	/// Tick of the moment where the message have been recieved.
	/// </summary>
	long Tick;
};