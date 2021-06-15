namespace Thrive.Src.General
{
    /// <summary>
    ///    A singleton used as a message queue, used by main menu to specify rules for a new game
    /// </summary>
    public class NewGameSetupData
    {
        public float Difficulty;
        private static NewGameSetupData instance;

        public NewGameSetupData(float difficulty)
        {
            Difficulty = difficulty;
        }

        public static void PushInstance(NewGameSetupData setupData)
        {
            instance = setupData;
        }

        public static NewGameSetupData PopInstance()
        {
            var instanceToReturn = instance;
            instance = null;
            return instanceToReturn;
        }
    }
}
