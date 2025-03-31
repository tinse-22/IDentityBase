﻿namespace AuthenticationApi.Application.Common
{
    public class ApiResult<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public static ApiResult<T> Success(T data) =>
            new ApiResult<T> { IsSuccess = true, Data = data};
        public static ApiResult<T> Failure(string error) =>
            new ApiResult<T> { IsSuccess = false, Message = error };
    }

}
