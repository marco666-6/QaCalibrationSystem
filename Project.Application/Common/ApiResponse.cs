using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Application.Common
{

    public sealed class ApiResponse<T>
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public T? Data { get; init; }
        public IEnumerable<string>? Errors { get; init; }

        public static ApiResponse<T> Ok(T data, string message = "Success")
            => new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Created(T data, string message = "Created successfully")
            => new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null)
            => new() { Success = false, Message = message, Errors = errors };

        public static ApiResponse<T> NotFound(string message = "Resource not found")
            => new() { Success = false, Message = message };
    }

    public sealed class ApiResponse
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public IEnumerable<string>? Errors { get; init; }

        public static ApiResponse Ok(string message = "Success")
            => new() { Success = true, Message = message };

        public static ApiResponse Fail(string message, IEnumerable<string>? errors = null)
            => new() { Success = false, Message = message, Errors = errors };

        public static ApiResponse NotFound(string message = "Resource not found")
            => new() { Success = false, Message = message };
    }

}
