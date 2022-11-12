public class DaylightProperties
{
    public float Maximum;
    public float Minimum;
    public float Current;
    public float Average;

    public DaylightProperties(float maximum, float current, float average)
    {
        Maximum = maximum;
        Current = current;
        Average = average;

        // Always zero for now, could be changed in future
        Minimum = 0.0f;
    }
}
