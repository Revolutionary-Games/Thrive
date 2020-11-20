using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

public class InputManager : Node
{
    private static InputManager singleton;
    private List<InputAttribute> allAttributes;

    public InputManager()
    {
        singleton = this;
        LoadAttributes(new[] { Assembly.GetExecutingAssembly() });
    }

    public static void AddInstance(object instance)
    {
        foreach (var inputAttribute in singleton.allAttributes.Where(p => p.Method.DeclaringType == instance.GetType()))
        {
            inputAttribute.AddInstance(new WeakReference(instance));
        }
    }

    public static void FocusLost()
    {
        singleton.allAttributes.ForEach(p => p.FocusLost());
    }

    public override void _Process(float delta)
    {
        allAttributes.ForEach(p => p.OnProcess(delta));
    }

    public override void _Input(InputEvent @event)
    {
        var result = false;
        allAttributes.ForEach(p =>
        {
            if (p.OnInput(@event))
                result = true;
        });

        if (result)
            GetTree().SetInputAsHandled();
    }

    private void LoadAttributes(IEnumerable<Assembly> assemblies)
    {
        allAttributes = new List<InputAttribute>();
        foreach (var assembly in assemblies.AsParallel())
        {
            foreach (var type in assembly.GetTypes().AsParallel())
            {
                foreach (var methodInfo in type.GetMethods().AsParallel())
                {
                    var attributes = (InputAttribute[])methodInfo.GetCustomAttributes(typeof(InputAttribute), true);
                    if (attributes.Length == 0)
                        continue;

                    var runOnAxisGroupAttribute =
                        (RunOnAxisGroupAttribute)attributes
                            .AsParallel()
                            .FirstOrDefault(p => p is RunOnAxisGroupAttribute);

                    foreach (var attribute in attributes)
                    {
                        attribute.Init(methodInfo);

                        if (runOnAxisGroupAttribute != null && attribute is RunOnAxisAttribute axis)
                            runOnAxisGroupAttribute.AddAxis(axis);
                        else
                            allAttributes.Add(attribute);
                    }
                }
            }
        }
    }
}
