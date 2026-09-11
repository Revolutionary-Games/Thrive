using System;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class ResourceLoadFrameBudgetTests
{
    [TestCase]
    public void OnlyTheFirstCompletionCanExceedTheAvailableBudget()
    {
        var budget = new ResourceLoadFrameBudget(0.6, 0, 1);

        AssertThat(budget.TryAdmit(0.5, 0)).IsTrue();
        AssertThat(budget.TryAdmit(0.5, 0.1)).IsFalse();
        AssertThat(budget.TryAdmit(0.2, 0.1)).IsTrue();
    }

    [TestCase]
    public void CompletionCannotStartWithZeroOrNegativeTimeRemaining()
    {
        var budget = new ResourceLoadFrameBudget(0, 0, 1);
        AssertThat(budget.TryAdmit(0, 1)).IsFalse();

        var indebtedBudget = new ResourceLoadFrameBudget(2, -1, 1);
        AssertThat(indebtedBudget.TryAdmit(0, 0)).IsFalse();
    }

    [TestCase]
    public void CarriedTimeIsBoundedInBothDirections()
    {
        var budget = new ResourceLoadFrameBudget(0, 0, 2);

        AssertThat(budget.CalculateSecondsToCarry(0)).IsEqual(1.0);
        AssertThat(budget.CalculateSecondsToCarry(20)).IsEqual(-4.0);
        AssertThat(budget.CalculateSecondsToCarry(1.5)).IsEqual(0.5);
    }

    [TestCase]
    public void NonFiniteArgumentsReportTheirOriginalParameterNames()
    {
        AssertThrown(() => new ResourceLoadFrameBudget(double.PositiveInfinity, 0, 1))
            .IsInstanceOf<ArgumentOutOfRangeException>()
            .HasPropertyValue(nameof(ArgumentOutOfRangeException.ParamName), "elapsedFrameTimeSeconds");
        AssertThrown(() => new ResourceLoadFrameBudget(0, double.NaN, 1))
            .IsInstanceOf<ArgumentOutOfRangeException>()
            .HasPropertyValue(nameof(ArgumentOutOfRangeException.ParamName), "savedProcessingTimeSeconds");
        AssertThrown(() => new ResourceLoadFrameBudget(0, 0, double.PositiveInfinity))
            .IsInstanceOf<ArgumentOutOfRangeException>()
            .HasPropertyValue(nameof(ArgumentOutOfRangeException.ParamName), "targetFrameTimeSeconds");

        var budget = new ResourceLoadFrameBudget(0, 0, 1);
        AssertThrown(() => budget.TryAdmit(double.PositiveInfinity, 0))
            .IsInstanceOf<ArgumentOutOfRangeException>()
            .HasPropertyValue(nameof(ArgumentOutOfRangeException.ParamName), "estimatedDurationSeconds");
    }
}
