/// <summary>
///   Something that can be constructed by a creature
/// </summary>
public interface IConstructable : IAcceptsResourceDeposit, IProgressReportableActionSource
{
    /// <summary>
    ///   True when this is completed and no longer needs to be built
    /// </summary>
    public bool Completed { get; }

    public bool HasRequiredResourcesToConstruct { get; }

    // TODO: default implementation of DepositActionAllowed once we use dotnet runtime to disallow when completed
}
