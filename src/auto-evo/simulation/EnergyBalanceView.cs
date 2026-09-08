namespace AutoEvo;

/// <summary>
///   Read access to an energy balance owned exclusively by the simulation cache.
///   The cache must never modify the backing result after publishing this view, including after Clear.
/// </summary>
internal readonly struct EnergyBalanceView
{
    private readonly EnergyBalanceInfoSimple balance;

    public EnergyBalanceView(EnergyBalanceInfoSimple balance)
    {
        this.balance = balance;
    }

    public float BaseMovement => balance.BaseMovement;

    public float Flagella => balance.Flagella;

    public float Actomyosin => balance.Actomyosin;

    public float Cilia => balance.Cilia;

    public float TotalMovement => balance.TotalMovement;

    public float Osmoregulation => balance.Osmoregulation;

    public float TotalProduction => balance.TotalProduction;

    public float TotalConsumption => balance.TotalConsumption;

    public float TotalConsumptionStationary => balance.TotalConsumptionStationary;

    public float FinalBalance => balance.FinalBalance;

    public float FinalBalanceStationary => balance.FinalBalanceStationary;

    public EnergyBalanceInfoSimple ToMutableCopy()
    {
        return new EnergyBalanceInfoSimple
        {
            BaseMovement = balance.BaseMovement,
            Flagella = balance.Flagella,
            Actomyosin = balance.Actomyosin,
            Cilia = balance.Cilia,
            TotalMovement = balance.TotalMovement,
            Osmoregulation = balance.Osmoregulation,
            TotalProduction = balance.TotalProduction,
            TotalConsumption = balance.TotalConsumption,
            TotalConsumptionStationary = balance.TotalConsumptionStationary,
            FinalBalance = balance.FinalBalance,
            FinalBalanceStationary = balance.FinalBalanceStationary,
        };
    }
}
