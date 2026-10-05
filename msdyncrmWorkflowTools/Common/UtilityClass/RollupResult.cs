namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// The results of <see cref="Utility.CalculateRollup"/>.
    /// </summary>
    public sealed class RollupResult
    {
        public RollupResult(decimal count, decimal sum, decimal average, decimal min, decimal max)
        {
            Count = count;
            Sum = sum;
            Average = average;
            Min = min;
            Max = max;
        }

        public decimal Count { get; }

        public decimal Sum { get; }

        public decimal Average { get; }

        public decimal Min { get; }

        public decimal Max { get; }
    }
}
