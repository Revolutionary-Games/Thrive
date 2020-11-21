using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public abstract class InputAttribute : Attribute
{
    private readonly List<WeakReference> instances = new List<WeakReference>();
    private readonly List<WeakReference> disposed = new List<WeakReference>();

    public MethodBase Method { get; private set; }

    public override bool Equals(object obj)
    {
        if (!(obj is InputAttribute attr))
            return false;
        return Equals(attr.Method, Method);
    }

    public override int GetHashCode()
    {
        return Method != null ? Method.GetHashCode() : 0;
    }

    public abstract bool OnInput(InputEvent input);
    public abstract void OnProcess(float delta);
    public abstract void FocusLost();

    internal void Init(MethodBase method)
    {
        Method = method;
    }

    internal void AddInstance(WeakReference instance)
    {
        instances.Add(instance);
    }

    protected bool CallMethod(params object[] parameters)
    {
        if (Method == null)
            return false;

        var result = false;
        Task.Run(() =>
        {
            lock (disposed)
            {
                disposed.Clear();
                if (Method.IsStatic)
                {
                    Method.Invoke(null, parameters);
                    result = true;
                }
                else
                {
                    instances.AsParallel().ForAll(p =>
                    {
                        result = true;
                        if (!p.IsAlive)
                        {
                            disposed.Add(p);
                            return;
                        }

                        Method.Invoke(p.Target, parameters);
                    });
                }

                disposed.AsParallel().ForAll(p => instances.Remove(p));
            }
        });
        return result;
    }
}
