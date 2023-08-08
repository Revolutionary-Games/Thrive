namespace Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DefaultEcs;
    using Godot;
    using Newtonsoft.Json;
    using Systems;

    /// <summary>
    ///   AI for a single Microbe (enables the <see cref="MicrobeAISystem"/>. to run on this). And also the memory for
    ///   the AI.
    /// </summary>
    public struct MicrobeAI
    {
        [JsonProperty]
        public float TimeUntilNextThink;

        [JsonProperty]
        public float PreviousAngle;

        [JsonProperty]
        public Vector3 TargetPosition;

        [JsonIgnore]
        public Entity FocusedPrey;

        [JsonIgnore]
        public Vector3? LastSmelledCompoundPosition;

        [JsonProperty]
        public float PursuitThreshold;

        /// <summary>
        ///   A value between 0.0f and 1.0f, this is the portion of the microbe's atp bar that needs to refill
        ///   before resuming motion.
        /// </summary>
        [JsonProperty]
        public float ATPThreshold;

        /// <summary>
        ///   Stores the value of microbe.totalAbsorbedCompound at tick t-1 before it is cleared and updated at tick t.
        ///   Used for compounds gradient computation.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Memory of the previous absorption step is required to compute gradient (which is a variation).
        ///     Values dictionary rather than single value as they will be combined with variable weights.
        ///   </para>
        /// </remarks>
        [JsonProperty]
        public Dictionary<Compound, float>? PreviouslyAbsorbedCompounds;

        [JsonIgnore]
        public Dictionary<Compound, float>? CompoundsSearchWeights;

        [JsonIgnore]
        public float TimeSinceSignalSniffing;

        [JsonIgnore]
        public Entity LastFoundSignalEmitter;

        [JsonIgnore]
        public MicrobeSignalCommand ReceivedCommand;

        [JsonProperty]
        public bool HasBeenNearPlayer;
    }

    public static class MicrobeAIHelpers
    {
        /// <summary>
        ///   Resets AI status when this AI controlled microbe is removed from a colony
        /// </summary>
        public static void ResetAI(this MicrobeAI ai)
        {
            ai.PreviousAngle = 0;
            ai.TargetPosition = Vector3.Zero;
            ai.FocusedPrey = default;
            ai.PursuitThreshold = 0;

            throw new NotImplementedException();

            // microbe.MovementDirection = Vector3.Zero;
            // microbe.TotalAbsorbedCompounds.Clear();
        }
    }
}
