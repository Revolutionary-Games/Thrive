using Godot;
using System;

public interface IFilterArgumentNode
{
    public void MakeSnapshot();
    public void RestoreLastSnapshot();
}
