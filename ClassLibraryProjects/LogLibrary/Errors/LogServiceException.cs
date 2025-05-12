/// <summary>
/// Custom exception class for logging service errors.
/// </summary>
public class LogServiceException : Exception
{
    /// <summary>
    /// Gets or sets the error code associated with the exception.
    /// </summary>
    public string ErrorCode { get; set; } = null!;

    /// <summary>
    /// Gets the timestamp when the exception was created.
    /// Defaults to the current date and time.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogServiceException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public LogServiceException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogServiceException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public LogServiceException(string message, Exception inner) : base(message, inner) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogServiceException"/> class with a specified error message and error code.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="code">The error code associated with the exception.</param>
    public LogServiceException(string message, string code) : base(message) => ErrorCode = code;

    /// <summary>
    /// Returns a string representation of the exception, including the timestamp, error code, and message.
    /// </summary>
    /// <returns>A string that represents the current exception.</returns>
    public override string ToString() => $"[{Timestamp}] {ErrorCode}: {Message}\n{base.ToString()}";
}
