/// <summary>
///   An error that was caught when loading a mod
/// </summary>
public class ModdingError
{
        /// <summary>
        ///   This constructor is used when the error is caught so early that a FullModDetails was not created yet.
        ///   Usually because the mod in question can not be found.
        /// </summary>
        public ModdingError(string name, string errorMessage)
        {
                ModInternalName = name;
                ErrorMessage = errorMessage;
        }

        public ModdingError(FullModDetails info, string errorMessage)
        {
                ModInternalName = info.InternalName;
                ModDetails = info;
                ErrorMessage = errorMessage;
        }

        /// <summary>
        ///   The name of the mod causing the error
        /// </summary>
        public string ModInternalName { get; }

        /// <summary>
        ///   THe mod that is causing the error
        /// </summary>
        public FullModDetails? ModDetails { get; }

        /// <summary>
        ///   The human readable error message to be displayed
        /// </summary>
        public string ErrorMessage { get; }
}
