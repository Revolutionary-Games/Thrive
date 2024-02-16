namespace Components
{
    using System;

    /// <summary>
    ///   Contains variables related to strain
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct StrainAffected
    {
        /// <summary>
        ///   True when sprinting or when strain is supposed to be otherwise generated
        /// </summary>
        public bool IsUnderStrain;

        /// <summary>
        ///   The current amount of strain
        /// </summary>
        public float CurrentStrain;

        /// <summary>
        ///   Strain above <see cref="Constants.MAX_STRAIN_PER_CELL"/>
        /// </summary>
        public float ExcessStrain;

        /// <summary>
        ///   The amount of time the player has to wait before <see cref="CurrentStrain"/> sarts to fall
        /// </summary>
        public float StrainDecreaseCooldown;
    }

    public static class StrainAffectedHelpers
    {
        public static float CalculateStrainFraction(this ref StrainAffected affected)
        {
            return Math.Max(0, affected.CurrentStrain - Constants.CANCELED_STRAIN) / Constants.MAX_STRAIN_PER_CELL;
        }
    }
}
