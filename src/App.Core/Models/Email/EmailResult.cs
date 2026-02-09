namespace App.Core.Models.Email;

/// <summary>
/// Result of sending an email
/// </summary>
public class EmailResult
{
    public bool Success { get; private set; }
    public string? Error { get; private set; }
    public Exception? Exception { get; private set; }

    private EmailResult(bool success, string? error = null, Exception? exception = null)
    {
        Success = success;
        Error = error;
        Exception = exception;
    }

    public static EmailResult Ok() => new(true);
    public static EmailResult Failed(string error) => new(false, error);
    public static EmailResult Failed(Exception ex) => new(false, ex.Message, ex);
}