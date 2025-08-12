using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace external_api_integration
{
    // Internal models for payment operations
    public class PaymentRequest
    {
        public string CardNumber { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        // Add other relevant fields
    }

    public class PaymentResponse
    {
        public string PaymentId { get; set; }
        public bool Success { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
    }

    public interface IPaymentService
    {
        Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request);
        Task<PaymentResponse> RefundPaymentAsync(string paymentId);
        Task<PaymentResponse> GetPaymentStatusAsync(string paymentId);
    }

    /// <summary>
    /// Service for integrating with a third-party payment API.
    /// Handles payment processing, refunds, and status checks with retry and logging.
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PaymentService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public PaymentService(HttpClient httpClient, ILogger<PaymentService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Configure Polly retry policy: 3 retries with exponential backoff
            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            exception,
                            "Retry {RetryCount} after {Delay}s due to error: {Message}",
                            retryCount,
                            timeSpan.TotalSeconds,
                            exception.Message
                        );
                    });
        }

        public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request)
        {
            var apiUrl = "https://thirdpartyapi.com/payments";
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    _logger.LogInformation("Sending payment request: {@Request}", request);
                    var response = await _httpClient.PostAsJsonAsync(apiUrl, request);

                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Received payment response: {Content}", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();
                        paymentResponse.Success = true;
                        return paymentResponse;
                    }
                    else
                    {
                        return new PaymentResponse
                        {
                            Success = false,
                            ErrorMessage = $"API Error: {response.StatusCode} - {content}"
                        };
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment processing failed.");
                return new PaymentResponse
                {
                    Success = false,
                    ErrorMessage = $"Exception: {ex.Message}"
                };
            }
        }

        public async Task<PaymentResponse> RefundPaymentAsync(string paymentId)
        {
            var apiUrl = $"https://thirdpartyapi.com/payments/{paymentId}/refund";
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    _logger.LogInformation("Initiating refund for PaymentId: {PaymentId}", paymentId);
                    var response = await _httpClient.PostAsync(apiUrl, null);

                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Received refund response: {Content}", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var refundResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();
                        refundResponse.Success = true;
                        return refundResponse;
                    }
                    else
                    {
                        return new PaymentResponse
                        {
                            Success = false,
                            ErrorMessage = $"API Error: {response.StatusCode} - {content}"
                        };
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refund failed for PaymentId: {PaymentId}", paymentId);
                return new PaymentResponse
                {
                    Success = false,
                    ErrorMessage = $"Exception: {ex.Message}"
                };
            }
        }

        public async Task<PaymentResponse> GetPaymentStatusAsync(string paymentId)
        {
            var apiUrl = $"https://thirdpartyapi.com/payments/{paymentId}/status";
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    _logger.LogInformation("Checking status for PaymentId: {PaymentId}", paymentId);
                    var response = await _httpClient.GetAsync(apiUrl);

                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Received status response: {Content}", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var statusResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();
                        statusResponse.Success = true;
                        return statusResponse;
                    }
                    else
                    {
                        return new PaymentResponse
                        {
                            Success = false,
                            ErrorMessage = $"API Error: {response.StatusCode} - {content}"
                        };
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Status check failed for PaymentId: {PaymentId}", paymentId);
                return new PaymentResponse
                {
                    Success = false,
                    ErrorMessage = $"Exception: {ex.Message}"
                };
            }
        }
    }
}
