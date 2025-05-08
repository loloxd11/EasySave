public class LogEntry
{
    public string Timestamp { get; set; }
    public string JobName { get; set; }
    public string Source { get; set; }
    public string Target { get; set; }
    public long FileSize { get; set; }
    public int TransferTimeMs { get; set; }
}
