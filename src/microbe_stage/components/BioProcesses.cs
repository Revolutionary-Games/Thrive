namespace Components
{
    using System.Collections.Generic;

    /// <summary>
    ///   Entity has bio processes to run by the <see cref="Systems.ProcessSystem"/>
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct BioProcesses
    {
        /// <summary>
        ///   The active processes that ProcessSystem handles
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     All processes that perform the same action should be combined together rather than listing that process
        ///     multiple times in this list (as that results in unexpected things as that isn't semantically how this
        ///     property is meant to be structured)
        ///   </para>
        /// </remarks>
        public List<TweakedProcess>? ActiveProcesses;

        /// <summary>
        ///   If set to not-null process statistics are gathered here
        /// </summary>
        public ProcessStatistics? ProcessStatistics;
    }
}
