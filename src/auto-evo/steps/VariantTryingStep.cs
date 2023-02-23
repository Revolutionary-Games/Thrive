namespace AutoEvo
{
    using System;

    /// <summary>
    ///   Base helper class for steps trying a fixed number of variant solutions and picking the best
    /// </summary>
    public abstract class VariantTryingStep : IRunStep
    {
        private int variantsToTry;
        private bool tryCurrentVariant;
        private bool storeSecondBest;

        private IAttemptResult? currentBest;
        private IAttemptResult? secondBest;

        protected VariantTryingStep(int variantsToTry, bool tryCurrentVariant, bool storeSecondBest = false)
        {
            this.variantsToTry = variantsToTry;
            this.tryCurrentVariant = tryCurrentVariant;
            this.storeSecondBest = storeSecondBest;
        }

        public interface IAttemptResult
        {
            public long Score { get; }
        }

        public int TotalSteps => (tryCurrentVariant ? 1 : 0) + variantsToTry;

        public abstract bool CanRunConcurrently { get; }

        public bool RunStep(RunResults results)
        {
            bool ran = false;

            if (tryCurrentVariant)
            {
                var result = TryCurrentVariant();

                CheckScore(result);

                tryCurrentVariant = false;
                ran = true;
            }

            if (variantsToTry > 0 && !ran)
            {
                var result = TryVariant();

                CheckScore(result);

                --variantsToTry;
            }

            if (!tryCurrentVariant && variantsToTry <= 0)
            {
                // Store the best result
                if (currentBest == null)
                    throw new Exception($"Variant step didn't try anything ({nameof(currentBest)} is null)");

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

        protected IAttemptResult? GetSecondBest()
        {
            return secondBest;
        }

        private void CheckScore(IAttemptResult result)
        {
            if (currentBest == null || result.Score > currentBest.Score)
            {
                if (storeSecondBest)
                    secondBest = currentBest;

                currentBest = result;
                return;
            }

            if (storeSecondBest && (secondBest == null || result.Score > secondBest.Score))
            {
                secondBest = result;
            }
        }
    }
}
