using System;
using System.Linq;
using System.Reflection;
using Godot;

public class InputManager : Node
{
    public override void _Ready()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var methodInfo in type.GetMethods())
                {
                    foreach (var attr in methodInfo.GetCustomAttributes(typeof(RunOnInputAttribute), true))
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
            p.Item2.InputReceiver.CheckInput(@event);
        });
    }

    public override void _Process(float delta)
    {
        RunOnInputAttribute.AttributesWithMethods.ForEach(p =>
        {
            var inputReceiver = p.Item2.InputReceiver;
            if (!inputReceiver.HasInput())
                return;

            var instance =
                RunOnInputAttribute.InputClasses.FirstOrDefault(x => x.GetType() == p.Item1.DeclaringType);
            if (!p.Item1.IsStatic && instance == null)
                return;

            switch (inputReceiver)
            {
                case InputMultiAxis _:
                case InputAxis _:
                    p.Item1.Invoke(instance, new[] { delta, inputReceiver.ReadInput() });
                    break;
                case InputTrigger _:
                case InputReleaseTrigger _:
                case InputHoldToggle _:
                    p.Item1.Invoke(instance, Array.Empty<object>());
                    break;
                case InputBool _:
                    p.Item1.Invoke(instance, new object[] { delta });
                    break;
            }
        });
    }
}
