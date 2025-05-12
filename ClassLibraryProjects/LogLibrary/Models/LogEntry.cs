/// <summary>
/// Represents a log entry containing details about a file transfer operation.
/// </summary>
public class LogEntry
{
    /// <summary>
    /// The timestamp of when the log entry was created, in string format.
    /// </summary>
    public required string Timestamp { get; set; }

    /// <summary>
    /// The name of the job associated with this log entry.
    /// </summary>
    public required string JobName { get; set; }

    /// <summary>
    /// The source location of the file being transferred.
    /// </summary>
    public required string Source { get; set; }

    /// <summary>
    /// The target location where the file is being transferred to.
    /// </summary>
    public required string Target { get; set; }

    /// <summary>
    /// The size of the file being transferred, in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// The time taken to transfer the file, in milliseconds.
    /// </summary>
    public int TransferTimeMs { get; set; }
}
