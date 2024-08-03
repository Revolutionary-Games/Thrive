﻿using System;

/// <summary>
///   Contains variables related to strain
/// </summary>
[JSONDynamicTypeAllowed]
public struct StrainAffected
{
    /// <summary>
    ///   The current amount of strain
    /// </summary>
    public float CurrentStrain;

    /// <summary>
    ///   The amount of time the organism has to wait before <see cref="CurrentStrain"/> sarts to fall
    /// </summary>
    public float StrainDecreaseCooldown;

    /// <summary>
    ///   True when sprinting or when strain is supposed to be otherwise generated
    /// </summary>
    public bool IsUnderStrain;
}

public static class StrainAffectedHelpers
{
    public static float CalculateStrainFraction(this ref StrainAffected affected)
    {
        return Math.Max(0, affected.CurrentStrain - Constants.CANCELED_STRAIN) / Constants.MAX_STRAIN_PER_ENTITY;
    }
}
