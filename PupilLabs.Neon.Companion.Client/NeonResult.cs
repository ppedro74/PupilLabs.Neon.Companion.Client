#nullable enable
namespace PupilLabs.Neon.Companion.Client
{
    using System.Net;

    public sealed class NeonResult<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public HttpStatusCode? StatusCode { get; }
        public string? ErrorMessage { get; }
        public string? ResponseBody { get; }

        private NeonResult(bool isSuccess, T? value, HttpStatusCode? statusCode, string? errorMessage, string? responseBody)
        {
            this.IsSuccess = isSuccess;
            this.Value = value;
            this.StatusCode = statusCode;
            this.ErrorMessage = errorMessage;
            this.ResponseBody = responseBody;
        }

        public static NeonResult<T> Success(T value, HttpStatusCode statusCode)
            => new NeonResult<T>(true, value, statusCode, null, null);

        public static NeonResult<T> Failure(string errorMessage, HttpStatusCode? statusCode = null, string? responseBody = null)
            => new NeonResult<T>(false, default, statusCode, errorMessage, responseBody);
    }
}