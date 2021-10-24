namespace AutoEvo
{
    /// <summary>
    ///   Base helper class for steps trying a fixed number of variant solutions and picking the best
    /// </summary>
    public abstract class VariantTryingStep : IRunStep
    {
        private int variantsToTry;
        private bool tryCurrentVariant;

        private IAttemptResult currentBest;

        protected VariantTryingStep(int variantsToTry, bool tryCurrentVariant)
        {
            this.variantsToTry = variantsToTry;
            this.tryCurrentVariant = tryCurrentVariant;
        }

        public interface IAttemptResult
        {
            long Score { get; }
        }

        public int TotalSteps => (tryCurrentVariant ? 1 : 0) + variantsToTry;

        public abstract bool CanRunConcurrently { get; }

        public bool RunStep(RunResults results)
        {
            bool ran = false;

            if (tryCurrentVariant)
            {
                var result = TryCurrentVariant();

                if (currentBest == null || result.Score > currentBest.Score)
                {
                    currentBest = result;
                }

                tryCurrentVariant = false;
                ran = true;
            }

            if (variantsToTry > 0 && !ran)
            {
                var result = TryVariant();

                if (currentBest == null || result.Score > currentBest.Score)
                {
                    currentBest = result;
                }

                --variantsToTry;
            }

            if (!tryCurrentVariant && variantsToTry <= 0)
            {
                // Store the best result
                OnBestResultFound(results, currentBest);
                return true;
            }

            return false;
        }

        /// <summary>
        ///   Generate and try a random variant
        /// </summary>
        protected abstract IAttemptResult TryVariant();

        /// <summary>
        ///   Try the "no action" choice
        /// </summary>
        protected abstract IAttemptResult TryCurrentVariant();

        /// <summary>
        ///   Called after the best attempted variant is determined
        /// </summary>
        /// <param name="results">Results to apply the found solution to.</param>
        /// <param name="bestVariant">Best variant found.</param>
        protected abstract void OnBestResultFound(RunResults results, IAttemptResult bestVariant);
    }
}
