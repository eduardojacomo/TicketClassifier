using TicketClassifier.Api.Dtos.Input;
using TicketClassifier.Api.Dtos.Output;
using TicketClassifier.Api.Gateways.Interface;
using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Mappers;

public static class TicketMapper
{
    public static Ticket ToEntity(TicketCsvInput input, Guid batchId, ClassificationResult c, bool ok) => new()
    {
        BatchId = batchId,
        ExternalId = input.ExternalId,
        Subject = input.Subject,
        Description = input.Description,
        Category = c.Category,
        Priority = c.Priority,
        Department = c.Department,
        Summary = c.Summary,
        Confidence = c.Confidence,
        Justification = c.Justification,
        Sentiment = c.Sentiment,
        Tags = System.Text.Json.JsonSerializer.Serialize(c.Tags),
        ProcessedOk = ok,
    };

    public static void ApplyResult(Ticket t, ClassificationResult c, bool ok)
    {
        t.Category = c.Category;
        t.Priority = c.Priority;
        t.Department = c.Department;
        t.Summary = c.Summary;
        t.Confidence = c.Confidence;
        t.Justification = c.Justification;
        t.Sentiment = c.Sentiment;
        t.Tags = System.Text.Json.JsonSerializer.Serialize(c.Tags);
        t.ProcessedOk = ok;
    }

    public static TicketDto ToDto(Ticket t, int similarCount = 0) => new()
    {
        Id = t.Id,
        ExternalId = t.ExternalId,
        Subject = t.Subject,
        Description = t.Description,
        Category = t.Category,
        Priority = t.Priority,
        Department = t.Department,
        Summary = t.Summary,
        Confidence = t.Confidence,
        Justification = t.Justification,
        Sentiment = t.Sentiment,
        Tags = ParseTags(t.Tags),
        ProcessedOk = t.ProcessedOk,
        RecordModified = t.RecordModified,
        ModifiedDate = t.ModifiedDate,
        SimilarCount = similarCount,
    };

    private static string[] ParseTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try { return System.Text.Json.JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>(); }
        catch { return Array.Empty<string>(); }
    }

    public static StatisticsDto ToStatistics(IReadOnlyCollection<Ticket> tickets) => new()
    {
        Total = tickets.Count,
        Failures = tickets.Count(x => !x.ProcessedOk),
        ByCategory = Categories.CategoryList.ToDictionary(c => c, c => tickets.Count(x => x.Category == c)),
        ByPriority = Categories.PriorityList.ToDictionary(p => p, p => tickets.Count(x => x.Priority == p)),
        ByDepartment = Categories.DepartmentList.ToDictionary(d => d, d => tickets.Count(x => x.Department == d)),
        BySentiment = Categories.SentimentList.ToDictionary(s => s, s => tickets.Count(x => x.Sentiment == s)),
        AverageConfidence = tickets.Count > 0 ? Math.Round(tickets.Average(x => x.Confidence), 2) : 0,
    };

    public static BatchSummaryDto ToSummary(TicketBatch b, IReadOnlyCollection<Ticket>? tickets = null) => new()
    {
        BatchId = b.Id,
        FileName = b.FileName,
        Total = b.Total,
        CreatedDate = b.CreatedDate,
        Statistics = tickets != null ? ToStatistics(tickets) : null,
    };

    public static BatchDetailDto ToDetail(TicketBatch b, IReadOnlyCollection<Ticket> tickets, Dictionary<Guid, int>? similarityCount = null)
    {
        return new()
        {
            BatchId = b.Id,
            FileName = b.FileName,
            Total = b.Total,
            CreatedDate = b.CreatedDate,
            Statistics = ToStatistics(tickets),
            Tickets = tickets.Select(t => ToDto(t, similarityCount?.GetValueOrDefault(t.Id) ?? 0)).ToList(),
        };
    }
}
