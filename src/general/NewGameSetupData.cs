namespace Thrive.src.general
{
    /// <summary>
    ///    A singleton used as a message queue, used by main menu to specify rules for a new game
    /// </summary>
    public class NewGameSetupData
    {
        private static NewGameSetupData Instance;

        public static void PushInstance(NewGameSetupData setupData)
        {
            Instance = setupData;
        }

        public static NewGameSetupData PopInstance()
        {
            var instanceToReturn = Instance;
            Instance = null;
            return instanceToReturn;
        }

        public float Difficulty;

        public NewGameSetupData(float Difficulty)
        {
            this.Difficulty = Difficulty;
        }

    }
}
