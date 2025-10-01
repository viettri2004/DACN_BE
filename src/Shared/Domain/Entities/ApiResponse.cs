using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace src.Shared.Domain.Entities
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }

        public ApiResponse() { }

        public ApiResponse(string code, string message, object? data = null, bool success = true)
        {
            Code = code;
            Message = message;
            Data = data;
            Success = success;
        }
    }
}