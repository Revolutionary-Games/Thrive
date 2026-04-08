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

    [TestCase]
    public void DistributeCompoundsDoesNotMakeNonUsefulInfinite()
    {
        // This is a test case against this bug:
        // https://community.revolutionarygamesstudio.com/t/infinite-toxins-in-multicellular/8970

        var world = World.Create();
        var firstBag = CreateBag(9.75f, Compound.Oxygen, Compound.Glucose, Compound.ATP,
            Compound.Carbondioxide, Compound.Iron, Compound.Nitrogen, Compound.Ammonia, Compound.Hydrogensulfide,
            Compound.Sunlight);
        var secondBag = CreateBag(10, Compound.Oxygen, Compound.Glucose, Compound.ATP,
            Compound.Carbondioxide, Compound.Iron, Compound.Oxytoxy, Compound.Nitrogen, Compound.Ammonia,
            Compound.Hydrogensulfide);
        var colonyBag = CreateColonyBag(world, firstBag, secondBag);

        SetAmount(firstBag, Compound.Glucose, 19.7498703f);
        SetAmount(firstBag, Compound.Iron, 0);
        SetAmount(firstBag, Compound.Hydrogensulfide, 9.74428368f);
        SetAmount(firstBag, Compound.Oxygen, 0.000984980026f);
        SetAmount(firstBag, Compound.ATP, 8.6438179f);

        // The problematic compound that shouldn't be in the first bag in the first place
        SetAmount(firstBag, Compound.Oxytoxy, 2.8125f);

        SetAmount(firstBag, Compound.Ammonia, 0.000100000063f);
        SetAmount(firstBag, Compound.Phosphates, 0.0905973688f);

        // Second bag setup
        SetAmount(secondBag, Compound.Ammonia, 0.00319526717f);
        SetAmount(secondBag, Compound.Phosphates, 0.0929203779f);
        SetAmount(secondBag, Compound.Hydrogensulfide, 9.99354553f);
        SetAmount(secondBag, Compound.Glucose, 9.99986935f);
        SetAmount(secondBag, Compound.Oxygen, 0.00101023586f);
        SetAmount(secondBag, Compound.Oxytoxy, 10);
        SetAmount(secondBag, Compound.ATP, 8.63854504f);

        colonyBag.DistributeCompoundSurplus();

        AssertThat(firstBag.GetCompoundAmount(Compound.Glucose)).IsEqual(14.686581f);
        AssertThat(firstBag.GetCompoundAmount(Compound.Hydrogensulfide)).IsEqual(9.743992f);
        AssertThat(firstBag.GetCompoundAmount(Compound.Iron)).IsEqual(0);
        AssertThat(firstBag.GetCompoundAmount(Compound.Oxygen)).IsEqual(0.000984980026f);
        AssertThat(firstBag.GetCompoundAmount(Compound.ATP)).IsEqual(8.6438179f);
        AssertThat(firstBag.GetCompoundAmount(Compound.Ammonia)).IsEqual(0.00162677746f);
        AssertThat(firstBag.GetCompoundAmount(Compound.Phosphates)).IsEqual(0.0905973688f);

        AssertThat(firstBag.GetCompoundAmount(Compound.Oxytoxy)).IsEqual(2.8125f);

        AssertThat(secondBag.GetCompoundAmount(Compound.Glucose)).IsEqual(10);
        AssertThat(secondBag.GetCompoundAmount(Compound.Hydrogensulfide)).IsEqual(9.99383736f);
        AssertThat(secondBag.GetCompoundAmount(Compound.Iron)).IsEqual(0);
        AssertThat(secondBag.GetCompoundAmount(Compound.Oxygen)).IsEqual(0.00101023586f);
        AssertThat(secondBag.GetCompoundAmount(Compound.ATP)).IsEqual(8.63854504f);
        AssertThat(secondBag.GetCompoundAmount(Compound.Ammonia)).IsEqual(0.00166848977f);
        AssertThat(secondBag.GetCompoundAmount(Compound.Phosphates)).IsEqual(0.0929203779f);

        AssertThat(secondBag.GetCompoundAmount(Compound.Oxytoxy)).IsEqual(10);

        // Make sure the oxytoxy can be consumed all (so that it isn't infinite)
        for (int i = 0; i < 1000; ++i)
        {
            var taken1 = secondBag.TakeCompound(Compound.Oxytoxy, 0.1f);

            if (taken1 <= 0)
                break;

            colonyBag.DistributeCompoundSurplus();
        }

        AssertThat(secondBag.GetCompoundAmount(Compound.Oxytoxy)).IsEqual(0);
        AssertThat(firstBag.GetCompoundAmount(Compound.Oxytoxy)).IsEqual(2.8125f);
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
