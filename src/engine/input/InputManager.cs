using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

/// <summary>
///   A handler for inputs.
///   This is an AutoLoad class.
///   Listens for inputs and invokes the input-attribute marked methods.
/// </summary>
public class InputManager : Node
{
    public override void _Ready()
    {
        RecalculateAttributes(new[] { Assembly.GetExecutingAssembly() });
        PauseMode = PauseModeEnum.Process;
    }

    /// <summary>
    ///   Primes all matching inputs with the correct GetValueForCallback() value.
    /// </summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        RunOnInputAttribute.AttributesWithMethods.ForEach(p => p.Item2.InputReceiver.CheckInput(@event));
    }

    /// <summary>
    ///   Reads all callback values where callbacks should be called and invokes the associated method.
    /// </summary>
    public override void _Process(float delta)
    {
        var disposed = new List<object>();
        RunOnInputAttribute.AttributesWithMethods.ForEach(p =>
        {
            var inputReceiver = p.Item2.InputReceiver;
            if (!inputReceiver.ShouldTriggerCallbacks())
                return;

            var readValue = inputReceiver.GetValueForCallback();

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

    /// <summary>
    ///   Try to invoke the given input method with the given data.
    /// </summary>
    /// <returns>False if the instance was disposed</returns>
    private static bool TryInvoke(MethodBase method, object instance, float delta, IInputReceiver inputReceiver,
        object readValue)
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
            if (ex.InnerException is ObjectDisposedException)
                return false;

            throw;
        }

        return true;
    }

    private static void RecalculateAttributes(Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                foreach (var methodInfo in type.GetMethods())
                {
                    var attributes = methodInfo.GetCustomAttributes(typeof(RunOnInputAttribute), true);
                    var multiAxis = attributes.FirstOrDefault(
                                                              p => p is RunOnMultiAxisAttribute) as RunOnMultiAxisAttribute;
                    foreach (var attr in attributes)
                    {
                        if (!(attr is RunOnInputAttribute myAttr))
                            continue;

                        if (attr is RunOnAxisAttribute axis && multiAxis != null)
                            multiAxis.DefinitionAttributes.Add(axis);
                        else
                        {
                            RunOnInputAttribute.AttributesWithMethods.Add(
                                                                          (methodInfo, myAttr));
                        }
                    }
                }
            }
        }
    }
}
