namespace Components
{
    using System.Collections.Generic;
    using Godot;

    /// <summary>
    ///   Entity has storage space for compounds
    /// </summary>
    public struct CompoundStorage
    {
        public CompoundBag Compounds;
    }

    public static class CompoundStorageHelpers
    {
        /// <summary>
        ///   Vent all remaining compounds immediately
        /// </summary>
        public static void VentAllCompounds(ref this CompoundStorage storage, Vector3 position,
            CompoundCloudSystem compoundClouds)
        {
            if (storage.Compounds.Compounds.Count > 0)
            {
                var keys = new List<Compound>(storage.Compounds.Compounds.Keys);

                foreach (var compound in keys)
                {
                    var amount = storage.Compounds.GetCompoundAmount(compound);
                    storage.Compounds.TakeCompound(compound, amount);

                    if (amount < MathUtils.EPSILON)
                        continue;

                    VentChunkCompound(ref storage, compound, amount, position, compoundClouds);
                }
            }
        }

        public static bool VentChunkCompound(ref this CompoundStorage storage, Compound compound, float amount,
            Vector3 position, CompoundCloudSystem compoundClouds)
        {
            amount = storage.Compounds.TakeCompound(compound, amount);

            if (amount <= 0)
                return false;

            compoundClouds.AddCloud(compound, amount * Constants.CHUNK_VENT_COMPOUND_MULTIPLIER, position);
            return amount > MathUtils.EPSILON;
        }
    }
}
