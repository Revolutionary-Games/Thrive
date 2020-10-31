using System.Collections.Generic;
using Godot;

public class InputDataList
{
    public InputDataList(Dictionary<string, List<InputEventWithModifiers>> data)
    {
        Data = data;
    }

    public Dictionary<string, List<InputEventWithModifiers>> Data { get; }

    public List<InputEventWithModifiers> this[string index] => Data[index];
}
