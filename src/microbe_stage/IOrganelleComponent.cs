﻿/// <summary>
///   Base interface that all organelle components need to implement
/// </summary>
public interface IOrganelleComponent
{
    void OnAttachToCell(PlacedOrganelle organelle);
    void OnDetachFromCell(PlacedOrganelle organelle);
    void Update(float elapsed);
}
