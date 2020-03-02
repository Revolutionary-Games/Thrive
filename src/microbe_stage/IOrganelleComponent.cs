using System;

/// <summary>
///   Base interface that all organelle components need to implement
/// </summary>
public interface IOrganelleComponent
{
    void OnAttachToCell();
    void OnDetachFromCell();
    void Update(float elapsed);
}
