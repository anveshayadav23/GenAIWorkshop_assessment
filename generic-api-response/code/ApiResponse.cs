using System;
using System.Text.Json.Serialization;

namespace Common.Models
{
    /// <summary>
    /// Represents a generic API response wrapper for consistent and structured API responses.
    /// </summary>
    /// <typeparam name="T">The type of the response data.</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Indicates if the API request was successful.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Message describing the result of the API request.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>
        /// Optional error code for failed responses.
        /// </summary>
        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; }

        /// <summary>
        /// The data returned by the API.
        /// </summary>
        [JsonPropertyName("data")]
        public T Data { get; set; }

        /// <summary>
        /// Timestamp of the response in UTC.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Unique request identifier.
        /// </summary>
        [JsonPropertyName("requestId")]
        public Guid RequestId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Pagination metadata (null if not paginated).
        /// </summary>
        [JsonPropertyName("pagination")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PaginationInfo Pagination { get; set; }

        /// <summary>
        /// Represents pagination metadata for paginated responses.
        /// </summary>
        public class PaginationInfo
        {
            /// <summary>
            /// Current page number.
            /// </summary>
            [JsonPropertyName("pageNumber")]
            public int PageNumber { get; set; }

            /// <summary>
            /// Number of items per page.
            /// </summary>
            [JsonPropertyName("pageSize")]
            public int PageSize { get; set; }

            /// <summary>
            /// Total number of records.
            /// </summary>
            [JsonPropertyName("totalRecords")]
            public int TotalRecords { get; set; }
        }

        /// <summary>
        /// Creates a successful API response without pagination.
        /// </summary>
        /// <param name="data">The response data.</param>
        /// <param name="message">Optional message.</param>
        /// <returns>An ApiResponse instance representing a successful response.</returns>
        public static ApiResponse<T> Success(T data, string message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message,
                ErrorCode = null,
                Pagination = null
            };
        }

        /// <summary>
        /// Creates an error API response.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="errorCode">Optional error code.</param>
        /// <param name="data">Optional data.</param>
        /// <returns>An ApiResponse instance representing an error response.</returns>
        public static ApiResponse<T> Error(string message, string errorCode = null, T data = default)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Data = data,
                Message = message,
                ErrorCode = errorCode,
                Pagination = null
            };
        }

        /// <summary>
        /// Creates a successful paginated API response.
        /// </summary>
        /// <param name="data">The response data.</param>
        /// <param name="pageNumber">Current page number.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="totalRecords">Total number of records.</param>
        /// <param name="message">Optional message.</param>
        /// <returns>An ApiResponse instance representing a paginated response.</returns>
        public static ApiResponse<T> WithPagination(T data, int pageNumber, int pageSize, int totalRecords, string message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message,
                ErrorCode = null,
                Pagination = new PaginationInfo
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = totalRecords
                }
            };
        }
    }
}