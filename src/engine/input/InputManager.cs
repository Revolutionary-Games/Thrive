using System;
using System.Linq;
using System.Reflection;
using Godot;

public class InputManager : Node
{
    static InputManager()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var methodInfo in type.GetMethods())
                {
                    foreach (var attr in methodInfo.GetCustomAttributes(typeof(RunOnInputAttribute), false))
                    {
                        if (!(attr is RunOnInputAttribute myAttr))
                            continue;
                        RunOnInputAttribute.AttributesWithMethods.Add(new Tuple<MethodBase, RunOnInputAttribute>(methodInfo, myAttr));
                    }
                }
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        RunOnInputAttribute.AttributesWithMethods.ForEach(p =>
        {
            var receiver = p.Item2.InputReceiver;
            receiver.CheckInput(@event);
            if (receiver.ReadInput())
                p.Item1.Invoke(null, Array.Empty<object>());
        });
    }
}
