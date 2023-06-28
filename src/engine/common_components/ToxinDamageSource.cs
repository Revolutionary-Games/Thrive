namespace Components
{
    /// <summary>
    ///   Defines toxin damage dealt by an entity
    /// </summary>
    public struct ToxinDamageSource
    {
        /// <summary>
        ///   Scales the damage
        /// </summary>
        public float ToxinAmount;

        public AgentProperties ToxinProperties;
    }
}
