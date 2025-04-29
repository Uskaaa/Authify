namespace Authify.Core.Common;

public class OperationResult<T> : OperationResult
{
    public T? Data { get; set; }

    public static OperationResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static new OperationResult<T> Fail(string error) => new() { Success = false, ErrorMessage = error };
}