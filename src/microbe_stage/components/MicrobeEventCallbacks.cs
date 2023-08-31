namespace Components
{
    using System;
    using System.Collections.Generic;
    using DefaultEcs;
    using Godot;

    /// <summary>
    ///   Entity that triggers various microbe event callbacks when things happens to it. This is mostly used for
    ///   connecting the player cell to the GUI and game stage.
    /// </summary>
    public struct MicrobeEventCallbacks
    {
        public Action<Entity>? OnUnbindEnabled;

        public Action<Entity>? OnUnbound;

        public Action<Entity, Entity>? OnIngestedByHostile;

        public Action<Entity, IEngulfable>? OnSuccessfulEngulfment;

        public Action<Entity>? OnEngulfmentStorageFull;

        public Action<Entity, IHUDMessage>? OnNoticeMessage;

        /// <summary>
        ///   Called when the reproduction status of this microbe changes
        /// </summary>
        public Action<Entity, bool>? OnReproductionStatus;

        /// <summary>
        ///   Called periodically to report the chemoreception settings of the microbe
        /// </summary>
        public Action<Entity, IEnumerable<(Compound Compound, float Range, float MinAmount, Color Colour)>>?
            OnCompoundChemoreceptionInfo;
    }
}
