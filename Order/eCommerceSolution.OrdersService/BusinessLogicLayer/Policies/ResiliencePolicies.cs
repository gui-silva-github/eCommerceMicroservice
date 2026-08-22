using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Polly;
using Polly.Bulkhead;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;
using System.Net;
using System.Net.Http.Json;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies
{
    /// <summary>
    /// Políticas async combinadas do Polly para HttpClientFactory.
    /// Fault tolerance entre o dependant (Orders) e as dependencies (Users / Products).
    /// </summary>
    public static class ResiliencePolicies
    {
        public const string FallbackHeader = "X-Fallback";

        public static IAsyncPolicy<HttpResponseMessage> CreateCombinedPolicy(
            string dependencyService,
            ResilienceOptions options)
        {
            AsyncTimeoutPolicy<HttpResponseMessage> timeout = Policy.TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromSeconds(options.TimeoutSeconds),
                TimeoutStrategy.Pessimistic);

            IAsyncPolicy<HttpResponseMessage> waitAndRetry = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .WaitAndRetryAsync(
                    retryCount: options.RetryCount,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt - 1)),
                    onRetry: (outcome, delay, retryAttempt, _) =>
                    {
                        Console.WriteLine(
                            $"[Polly][{dependencyService}] WaitAndRetry #{retryAttempt} em {delay.TotalMilliseconds:0}ms. " +
                            $"Erro: {Describe(outcome)}");
                    });

            IAsyncPolicy<HttpResponseMessage> circuitBreaker = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: options.ExceptionsAllowedBeforeBreaking,
                    durationOfBreak: TimeSpan.FromSeconds(options.DurationOfBreakSeconds),
                    onBreak: (outcome, breakDelay) =>
                    {
                        Console.WriteLine(
                            $"[Polly][{dependencyService}] CircuitBreaker ABERTO por {breakDelay.TotalSeconds:0}s. " +
                            $"Erro: {Describe(outcome)}");
                    },
                    onReset: () =>
                    {
                        Console.WriteLine($"[Polly][{dependencyService}] CircuitBreaker FECHADO (tráfego retomado).");
                    },
                    onHalfOpen: () =>
                    {
                        Console.WriteLine($"[Polly][{dependencyService}] CircuitBreaker MEIO-ABERTO (probe).");
                    });

            AsyncBulkheadPolicy<HttpResponseMessage> bulkhead = Policy.BulkheadAsync<HttpResponseMessage>(
                maxParallelization: options.MaxParallelization,
                maxQueuingActions: options.MaxQueuingActions,
                onBulkheadRejectedAsync: _ =>
                {
                    Console.WriteLine(
                        $"[Polly][{dependencyService}] Bulkhead rejeitou a chamada " +
                        $"(maxParallelization={options.MaxParallelization}, maxQueuingActions={options.MaxQueuingActions}).");
                    return Task.CompletedTask;
                });

            IAsyncPolicy<HttpResponseMessage> fallback = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<TimeoutRejectedException>()
                .Or<BrokenCircuitException>()
                .Or<BulkheadRejectedException>()
                .OrTransientHttpError()
                .FallbackAsync(
                    (DelegateResult<HttpResponseMessage> outcome, Context _, CancellationToken __) =>
                    {
                        FaultDTO fault = new(
                            Dependant: "OrdersService",
                            Dependency: dependencyService,
                            Message: $"{dependencyService} indisponível. Fallback Polly acionado.",
                            FaultType: outcome.Exception?.GetType().Name ?? "DependencyFailure",
                            OccurredAtUtc: DateTime.UtcNow,
                            UsedFallback: true);

                        Console.WriteLine(
                            $"[Polly][{dependencyService}] Fallback: {fault.FaultType}: {fault.Message}");

                        HttpResponseMessage response = new(HttpStatusCode.ServiceUnavailable)
                        {
                            Content = JsonContent.Create(fault)
                        };
                        response.Headers.Add(FallbackHeader, "true");
                        return Task.FromResult(response);
                    },
                    (DelegateResult<HttpResponseMessage> outcome, Context _) =>
                    {
                        Console.WriteLine(
                            $"[Polly][{dependencyService}] onFallback: {Describe(outcome)}");
                        return Task.CompletedTask;
                    });

            // Combined policies (outer → inner): Fallback → CircuitBreaker → WaitAndRetry → Bulkhead → Timeout
            return Policy.WrapAsync(fallback, circuitBreaker, waitAndRetry, bulkhead, timeout);
        }

        private static string Describe(DelegateResult<HttpResponseMessage> outcome)
        {
            if (outcome.Exception != null)
            {
                return outcome.Exception.GetType().Name + ": " + outcome.Exception.Message;
            }

            return outcome.Result == null
                ? "sem resposta"
                : $"HTTP {(int)outcome.Result.StatusCode} {outcome.Result.StatusCode}";
        }
    }
}
