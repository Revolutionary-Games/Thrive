using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

public class InputManager : Node
{
    public override void _Ready()
    {
        RecalculateAttributes();
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
        var disposed = new List<object>();
        RunOnInputAttribute.AttributesWithMethods.ForEach(p =>
        {
            var inputReceiver = p.Item2.InputReceiver;
            if (!inputReceiver.HasInput())
                return;

            var readValue = inputReceiver.ReadInput();

            if (p.Item1.IsStatic)
            {
                TryInvoke(p.Item1, null, delta, inputReceiver, readValue);
            }
            else
            {
                var instances =
                    RunOnInputAttribute.InputClasses.Where(x => x.GetType() == p.Item1.DeclaringType).ToList();
                foreach (var instance in instances)
                {
                    if (!TryInvoke(p.Item1, instance, delta, inputReceiver, readValue))
                        disposed.Add(instance);
                }
            }
        });
        disposed.ForEach(p => RunOnInputAttribute.InputClasses.Remove(p));
    }

    /// <returns>False if the instance was disposed</returns>
    private static bool TryInvoke(
        MethodBase method, object instance, float delta, IInputReceiver inputReceiver, object readValue)
    {
        try
        {
            switch (inputReceiver)
            {
                case InputMultiAxis _:
                case InputAxis _:
                    method.Invoke(instance, new[] { delta, readValue });
                    break;
                case InputTrigger _:
                case InputReleaseTrigger _:
                    method.Invoke(instance, Array.Empty<object>());
                    break;
                case InputHoldToggle _:
                case InputBool _:
                    method.Invoke(instance, new object[] { delta });
                    break;
            }
        }
        catch (TargetInvocationException ex)
        {
            // Disposed object
            return !(ex.InnerException is ObjectDisposedException);
        }

        return true;
    }

    private static void RecalculateAttributes()
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
                        RunOnInputAttribute.AttributesWithMethods.Add(
                        new Tuple<MethodBase, RunOnInputAttribute>(methodInfo, myAttr));
                    }
                }
            }
        }
    }
}
