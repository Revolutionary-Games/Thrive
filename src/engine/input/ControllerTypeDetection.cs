public static class ControllerTypeDetection
{
    /// <summary>
    ///   Detects the type of a controlled (gamepad, joypad) from its name
    /// </summary>
    /// <param name="name">The name of the controller to detect</param>
    /// <param name="defaultResult">The default value to return if detection fails</param>
    /// <returns>The type of controller used</returns>
    public static ControllerType DetectControllerTypeFromName(string name,
        ControllerType defaultResult = Constants.DEFAULT_CONTROLLER_TYPE)
    {
        // To simplify checking we convert the name to lowercase
        name = name.ToLowerInvariant();

        // ReSharper disable CommentTypo StringLiteralTypo
        // SDL_gamecontrollerdb.h and gamecontrollerdb.txt are good sources for names. For example at the following URL
        // https://github.com/gabomdq/SDL_GameControllerDB/blob/master/gamecontrollerdb.txt

        // PlayStation controllers
        if (name.Contains("dualsense"))
            return ControllerType.PlayStation5;

        if (name.Contains("sony") || name.Contains("playstation") || name.Contains("dualshock"))
        {
            // PlayStation controller
            if (name.Contains("4"))
            {
                return ControllerType.PlayStation4;
            }

            if (name.Contains("3"))
            {
                return ControllerType.PlayStation3;
            }

            // Let's make PS5 the default
            return ControllerType.PlayStation5;
        }

        // Xbox controllers
        if (name.Contains("xbox") || name.Contains("microsoft"))
        {
            if (name.Contains("xbox one"))
                return ControllerType.XboxOne;

            if (name.Contains("series s") || name.Contains("series x"))
                return ControllerType.XboxSeriesX;

            // This detection was more broad before but didn't work well, mainly because when bluetooth wireless
            // the xbox controller is not well detected on Linux
            if (name.Contains("360"))
                return ControllerType.Xbox360;

            if (name.Contains("one"))
                return ControllerType.XboxOne;

            // Default to latest if the controller isn't probably an older one
            return ControllerType.XboxSeriesX;
        }

        // Shorter name PlayStation matching
        if (name.Contains("ps1") || name.Contains("ps2") || name.Contains("ps3"))
        {
            return ControllerType.PlayStation3;
        }

        if (name.Contains("ps4"))
        {
            return ControllerType.PlayStation4;
        }

        if (name.Contains("ps5"))
        {
            return ControllerType.PlayStation5;
        }

        // TODO: Nintendo and Steam controllers

        // ReSharper restore CommentTypo StringLiteralTypo

        return defaultResult;
    }
}
