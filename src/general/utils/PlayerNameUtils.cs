using System;
using Godot;

/// <summary>
///   Helpers for validating names entered by the player.
/// </summary>
public static class PlayerNameUtils
{
    public const int DEFAULT_MAXIMUM_PLAYER_NAME_LENGTH = 80;

    private static readonly char[] InvalidNodeNameCharacters = ['/', ':', '%', '@'];

    /// <summary>
    ///   Checks whether a player-given name is safe to use as a Godot node name.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <param name="errorMessage">
    ///   An explanation of why the name is invalid, or an empty string if it is valid.
    /// </param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <returns>True when the name is valid.</returns>
    public static bool IsValidPlayerGivenName(string name, out string errorMessage,
        int maxLength = DEFAULT_MAXIMUM_PLAYER_NAME_LENGTH)
    {
        if (maxLength < 1)
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Maximum name length must be positive");

        if (string.IsNullOrWhiteSpace(name))
        {
            errorMessage = Localization.Translate("PLAYER_NAME_CANNOT_BE_EMPTY");
            return false;
        }

        if (name.Length > maxLength)
        {
            errorMessage = Localization.Translate("PLAYER_NAME_TOO_LONG").FormatSafe(maxLength);
            return false;
        }

        if (name.IndexOfAny(InvalidNodeNameCharacters) >= 0 || name is "." or "..")
        {
            errorMessage = Localization.Translate("PLAYER_NAME_HAS_INVALID_NODE_CHARACTERS");
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
