namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies
{
    public class ResilienceOptions
    {
        public const string SectionName = "Resilience";

        public int RetryCount { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 3;
        public int ExceptionsAllowedBeforeBreaking { get; set; } = 3;
        public int DurationOfBreakSeconds { get; set; } = 15;
        public int MaxParallelization { get; set; } = 5;
        public int MaxQueuingActions { get; set; } = 10;
        public int CacheSeconds { get; set; } = 60;
    }
}
