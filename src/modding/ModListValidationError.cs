using static ModLoader;

/// <summary>
///   An error that was found the mod loader checker
/// </summary>
public class ModListValidationError
{
        public ModListValidationError(CheckErrorStatus errorType)
        {
                ErrorType = errorType;
        }

        public ModListValidationError(CheckErrorStatus errorType, ModInfo? checkedMod)
        {
                ErrorType = errorType;
                CheckedMod = checkedMod;
        }

        public ModListValidationError(CheckErrorStatus errorType, ModInfo? checkedMod, ModInfo? otherMod)
        {
                ErrorType = errorType;
                CheckedMod = checkedMod;
                OtherMod = otherMod;
        }

        /// <summary>
        ///   The type of error that was found
        /// </summary>
        public CheckErrorStatus ErrorType { get; }

        /// <summary>
        ///   The mod that was being checked by the mod loader
        /// </summary>
        public ModInfo? CheckedMod { get; }

        /// <summary>
        ///   A mod that is causing the checked mod to fail
        /// </summary>
        public ModInfo? OtherMod { get; }
}
