using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Components;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class ColonyCompoundBagTests
{
    [TestCase]
    public void DistributeCompoundSurplusBalancesUsefulCompoundsByCapacity()
    {
        var world = World.Create();
        var firstBag = CreateBag(10, Compound.Ammonia);
        var secondBag = CreateBag(20, Compound.Ammonia);
        var colonyBag = CreateColonyBag(world, firstBag, secondBag);

        SetAmount(firstBag, Compound.Ammonia, 15);

        colonyBag.DistributeCompoundSurplus();

        AssertThat(firstBag.GetCompoundAmount(Compound.Ammonia)).IsEqual(5.0f);
        AssertThat(secondBag.GetCompoundAmount(Compound.Ammonia)).IsEqual(10.0f);
    }

    [TestCase]
    public void DistributeCompoundSurplusLeavesAlreadyBalancedCompoundsUntouched()
    {
        var world = World.Create();
        var firstBag = CreateBag(10, Compound.Ammonia);
        var secondBag = CreateBag(20, Compound.Ammonia);
        var colonyBag = CreateColonyBag(world, firstBag, secondBag);

        SetAmount(firstBag, Compound.Ammonia, 10);
        SetAmount(secondBag, Compound.Ammonia, 20);

        colonyBag.DistributeCompoundSurplus();

        AssertThat(firstBag.GetCompoundAmount(Compound.Ammonia)).IsEqual(10.0f);
        AssertThat(secondBag.GetCompoundAmount(Compound.Ammonia)).IsEqual(20.0f);
    }

    [TestCase]
    public void DistributeCompoundSurplusLeavesNonDistributableCompoundsUntouched()
    {
        var world = World.Create();
        var firstBag = CreateBag(10);
        var secondBag = CreateBag(20);
        var colonyBag = CreateColonyBag(world, firstBag, secondBag);

        SetAmount(firstBag, Compound.ATP, 15);

        colonyBag.DistributeCompoundSurplus();

        AssertThat(firstBag.GetCompoundAmount(Compound.ATP)).IsEqual(15.0f);
        AssertThat(secondBag.GetCompoundAmount(Compound.ATP)).IsEqual(0.0f);
    }

    [TestCase]
    public void DistributeCompoundSurplusSkipsCompoundsThatAreNotUsefulAnywhere()
    {
        var world = World.Create();
        var firstBag = CreateBag(10);
        var secondBag = CreateBag(20);
        var colonyBag = CreateColonyBag(world, firstBag, secondBag);

        SetAmount(firstBag, Compound.Glucose, 9);

        colonyBag.DistributeCompoundSurplus();

        AssertThat(firstBag.GetCompoundAmount(Compound.Glucose)).IsEqual(9.0f);
        AssertThat(secondBag.GetCompoundAmount(Compound.Glucose)).IsEqual(0.0f);
    }

    [TestCase]
    public void DistributeCompoundSurplusClearsSummedCompoundsBufferBetweenRuns()
    {
        var world = World.Create();
        var firstBag = CreateBag(10, Compound.Ammonia, Compound.Phosphates);
        var secondBag = CreateBag(20, Compound.Ammonia, Compound.Phosphates);
        var colonyBag = CreateColonyBag(world, firstBag, secondBag);

        SetAmount(firstBag, Compound.Ammonia, 15);
        colonyBag.DistributeCompoundSurplus();

        firstBag.ClearCompounds();
        secondBag.ClearCompounds();
        SetAmount(firstBag, Compound.Phosphates, 6);

        colonyBag.DistributeCompoundSurplus();

        AssertThat(firstBag.GetCompoundAmount(Compound.Ammonia)).IsEqual(0.0f);
        AssertThat(secondBag.GetCompoundAmount(Compound.Ammonia)).IsEqual(0.0f);
        AssertThat(firstBag.GetCompoundAmount(Compound.Phosphates)).IsEqual(2.0f);
        AssertThat(secondBag.GetCompoundAmount(Compound.Phosphates)).IsEqual(4.0f);
    }

    [TestCase]
    public void DistributeCompoundSurplusSkipsUsefulCompoundsWithZeroCapacity()
    {
        var world = World.Create();
        var firstBag = CreateBag(0, Compound.Glucose);
        var secondBag = CreateBag(0, Compound.Glucose);
        var colonyBag = CreateColonyBag(world, firstBag, secondBag);

        SetAmount(firstBag, Compound.Glucose, 3);

        colonyBag.DistributeCompoundSurplus();

        AssertThat(firstBag.GetCompoundAmount(Compound.Glucose)).IsEqual(3.0f);
        AssertThat(secondBag.GetCompoundAmount(Compound.Glucose)).IsEqual(0.0f);
    }

    private static CompoundBag CreateBag(float nominalCapacity, params Compound[] usefulCompounds)
    {
        var bag = new CompoundBag(nominalCapacity);

        foreach (var usefulCompound in usefulCompounds)
        {
            bag.SetUseful(usefulCompound);
        }

        return bag;
    }

    private static ColonyCompoundBag CreateColonyBag(World world, params CompoundBag[] bags)
    {
        var colonyMembers = new Entity[bags.Length];

        for (int i = 0; i < bags.Length; ++i)
        {
            var entity = world.Create();
            entity.Add(new CompoundStorage { Compounds = bags[i] });
            colonyMembers[i] = entity;
        }

        return new ColonyCompoundBag(colonyMembers);
    }

    private static void SetAmount(CompoundBag bag, Compound compound, float amount)
    {
        bag.AddInitialCompounds(new Dictionary<Compound, float>
        {
            [compound] = amount,
        });
    }
}
