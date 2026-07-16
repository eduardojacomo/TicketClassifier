namespace TicketClassifier.Api.Dtos.Output;

public class TicketDto
{
    public Guid Id { get; set; }
    public string? ExternalId { get; set; }
    public string? Subject { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Justification { get; set; } = string.Empty;
    public string Sentiment { get; set; } = "neutral";
    public string[] Tags { get; set; } = Array.Empty<string>();
    public bool ProcessedOk { get; set; }
    public bool RecordModified { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int SimilarCount { get; set; }
}

public class TicketEditDto
{
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public string? Department { get; set; }
    public string? Sentiment { get; set; }
    public string[]? Tags { get; set; }
}

public class StatisticsDto
{
    public int Total { get; set; }
    public int Failures { get; set; }
    public Dictionary<string, int> ByCategory { get; set; } = new();
    public Dictionary<string, int> ByPriority { get; set; } = new();
    public Dictionary<string, int> ByDepartment { get; set; } = new();
    public Dictionary<string, int> BySentiment { get; set; } = new();
    public double AverageConfidence { get; set; }
}

/// <summary>Batch summary (listing and upload return).</summary>
public class BatchSummaryDto
{
    public Guid BatchId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int Total { get; set; }
    public DateTime CreatedDate { get; set; }
    public StatisticsDto? Statistics { get; set; }
}

/// <summary>Batch detail: summary + tickets.</summary>
public class BatchDetailDto
{
    public Guid BatchId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int Total { get; set; }
    public DateTime CreatedDate { get; set; }
    public StatisticsDto Statistics { get; set; } = new();
    public List<TicketDto> Tickets { get; set; } = new();
}
