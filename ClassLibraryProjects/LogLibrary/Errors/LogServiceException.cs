public class LogServiceException : Exception
{
    public string ErrorCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public LogServiceException(string message) : base(message) { }
    public LogServiceException(string message, Exception inner) : base(message, inner) { }
    public LogServiceException(string message, string code) : base(message) => ErrorCode = code;

    public override string ToString() => $"[{Timestamp}] {ErrorCode}: {Message}\n{base.ToString()}";
}
