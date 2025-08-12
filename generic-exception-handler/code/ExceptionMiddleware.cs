using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace global_exception_handling
{
    /// <summary>
    /// Middleware for global exception handling.
    /// Maps exceptions to consistent JSON responses and logs details.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, _logger);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger logger)
        {
            int statusCode;
            string message;
            string details = null;

            // Map exception types to status codes and messages
            switch (exception)
            {
                case ValidationException ve:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = ve.Message;
                    details = ve.InnerException?.Message;
                    break;
                case BusinessLogicException be:
                    statusCode = 422; // Unprocessable Entity
                    message = be.Message;
                    details = be.InnerException?.Message;
                    break;
                default:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    message = "An unexpected error occurred.";
                    details = exception.Message;
                    break;
            }

            logger.LogError(exception, "Exception caught by global middleware: {Message}", exception.Message);

            var response = new
            {
                success = false,
                message,
                details
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

    // Custom exception for validation errors
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }

    // Custom exception for business logic errors
    public class BusinessLogicException : Exception
    {
        public BusinessLogicException(string message) : base(message) { }
    }
}