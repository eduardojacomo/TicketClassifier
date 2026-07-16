using System.Collections.Concurrent;

namespace TicketClassifier.Api.Services;

public class Progress
{
    public int Total { get; set; }
    public int Processed { get; set; }
    public int Ok { get; set; }
    public int Failures { get; set; }
    public int TotalBatches { get; set; }
    public int BatchesCompleted { get; set; }
    public bool Completed { get; set; }
    public Guid? BatchId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}

public interface IProgressStore
{
    void Start(Guid jobId, int total, int totalBatches);
    void ReportBatch(Guid jobId, int processed, int ok);
    void Complete(Guid jobId, Guid batchId);
    Progress? Get(Guid jobId);
}

/// <summary>
/// Tracks processing progress per jobId (in memory). The frontend
/// polls during the upload to display counters, the bar and the log.
/// </summary>
public class ProgressStore : IProgressStore
{
    private readonly ConcurrentDictionary<Guid, Progress> _map = new();

    public void Start(Guid jobId, int total, int totalBatches)
        => _map[jobId] = new Progress { Total = total, TotalBatches = totalBatches };

    public void ReportBatch(Guid jobId, int processed, int ok)
    {
        if (_map.TryGetValue(jobId, out var p))
            lock (p)
            {
                p.Processed += processed;
                p.Ok += ok;
                p.Failures += processed - ok;
                p.BatchesCompleted++;
            }
    }

    public void Complete(Guid jobId, Guid batchId)
    {
        if (_map.TryGetValue(jobId, out var p))
            lock (p)
            {
                p.Processed = p.Total;
                p.Completed = true;
                p.BatchId = batchId;
            }
        // Delayed cleanup so the frontend can read the final state.
        _ = Task.Delay(TimeSpan.FromMinutes(2)).ContinueWith(t => _map.TryRemove(jobId, out _));
    }

    public Progress? Get(Guid jobId) => _map.GetValueOrDefault(jobId);
}
