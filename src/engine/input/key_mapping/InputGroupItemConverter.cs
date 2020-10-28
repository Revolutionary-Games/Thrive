using System;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

public class InputGroupItemConverter : JsonConverter<InputGroupItem>
{
    public override void WriteJson(JsonWriter writer, InputGroupItem value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    public override InputGroupItem ReadJson(JsonReader reader, Type objectType, InputGroupItem existingValue, bool hasExistingValue,
                                            JsonSerializer serializer)
    {
        var inputGroupItem = (InputGroupItem)InputGroupList.InputGroupItemScene.Instance();
        serializer.Populate(reader, inputGroupItem);
        foreach (var inputActionItem in inputGroupItem.Actions)
        {
            inputActionItem.AssociatedGroup = inputGroupItem;
            inputActionItem.Inputs = new ObservableCollection<InputEventItem>(InputGroupList.Instance.LoadingData[inputActionItem.InputName].Select(p =>
            {
                var inputEventItem = (InputEventItem)InputGroupList.InputEventItemScene.Instance();
                inputEventItem.AssociatedAction = inputActionItem;
                inputEventItem.AssociatedEvent = p;
                return inputEventItem;
            }));
        }

        return inputGroupItem;
    }
}

