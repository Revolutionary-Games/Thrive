using System.Collections;
using Godot;

public class DayNightCycle
{
    /*

    * stuff we need at a minimum / TL;DR
        exact time
        states for dawn, day, dusk, and night
        config parameters; a JSON file
        methods to feed timeOfDay to relevant shaders
        methods to manipulate environment node for lighting and post-process

    * for simplicity's sake, the time can just be a float property (hour.minute)
        public float Time{ get; set; };

    * a state machine is the natural solution for this class, maybe state pattern too.
      doesn't necessarily have to be an enum and switch state machine specifically.
        public enum TimeOfDay
        {
            Dawn,
            Day,
            Dusk,
            Night
        }

        public TimeOfDay timeOfDay;

        switch(timeOfDay)
            case TimeOfDay.Dawn
            {
                Patch.LightLevel = DawnLightLevel(this.Time);
                break;
            }

            case TimeOfDay.Day
            {
                if (Patch.LightLevel != DAY_LIGHT_LEVEL)
                    Patch.LightLevel = DAY_LIGHT_LEVEL;
                else
                    break;
            }

            case TimeOfDay.Dusk
            {
                Patch.LightLevel = DuskLightLevel(this.Time);
                break;
            }

            case TimeOfDay.Night
            {
                if (Patch.LightLevel != NIGHT_LIGHT_LEVEL)
                    Patch.LightLevel = NIGHT_LIGHT_LEVEL;
                else
                    break;
            }

    * we need various parameters that can be changed on a per planet basis like:
        how long a day is
        light levels for day and night if not const
        graphical particularities like color during sunrise/sunset, when to apply post fx

    */
}
