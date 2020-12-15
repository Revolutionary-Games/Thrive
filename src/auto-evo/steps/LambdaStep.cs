namespace AutoEvo
{
    using System;

    /// <summary>
    ///   Runs a generic function as a step.
    ///   Always assumed to be finished on the first call so the func is only called once.
    ///   Meant for one-off steps. If a similar step is used multiple times it should be made into a class
    /// </summary>
    public class LambdaStep : IRunStep
    {
        private readonly Action<RunResults> operation;

        public LambdaStep(Action<RunResults> operation)
        {
            this.operation = operation;
        }

        public int TotalSteps => 1;

        public bool RunStep(RunResults results)
        {
            operation(results);
            return true;
        }
    }
}
