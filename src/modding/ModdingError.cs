using System;
public class ModdingError
{
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

        public string ModInternalName { get; }
        public FullModDetails? ModDetails { get; }
        public string ErrorMessage { get; }
}
