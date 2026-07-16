using Microsoft.AspNetCore.Mvc;
using TicketClassifier.Api.Dtos.Output;
using TicketClassifier.Api.Services;
using TicketClassifier.Api.Services.Interface;

namespace TicketClassifier.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _service;
    private readonly IProgressStore _progress;

    public TicketsController(ITicketService service, IProgressStore progress)
    {
        _service = service;
        _progress = progress;
    }

    [HttpGet("check-duplicate")]
    public async Task<IActionResult> CheckDuplicate([FromQuery] string fileName, CancellationToken ct)
    {
        var existing = await _service.CheckDuplicateAsync(fileName, ct);
        return Ok(new { duplicate = existing is not null, batch = existing });
    }

    [HttpPost("upload")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] Guid? jobId, [FromForm] bool overwrite, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded." });

        try
        {
            using var stream = file.OpenReadStream();
            var summary = await _service.ProcessCsvAsync(stream, file.FileName, jobId ?? Guid.NewGuid(), overwrite, ct);
            return Ok(summary);
        }
        catch (CsvValidationException ex)
        {
            return BadRequest(new { error = ex.Message, code = ex.Code });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Processing progress for an upload (for the bar/counter).</summary>
    [HttpGet("progress/{jobId:guid}")]
    public IActionResult Progress(Guid jobId)
    {
        var p = _progress.Get(jobId);
        return Ok(new
        {
            total = p?.Total ?? 0,
            processed = p?.Processed ?? 0,
            ok = p?.Ok ?? 0,
            failures = p?.Failures ?? 0,
            totalBatches = p?.TotalBatches ?? 0,
            batchesCompleted = p?.BatchesCompleted ?? 0,
            completed = p?.Completed ?? false,
            batchId = p?.BatchId,
        });
    }

    [HttpGet("batches")]
    public async Task<IActionResult> Batches(CancellationToken ct)
        => Ok(await _service.ListAsync(ct));

    [HttpGet("batches/{id:guid}")]
    public async Task<IActionResult> Batch(Guid id, CancellationToken ct)
    {
        var detail = await _service.GetAsync(id, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost("batches/{id:guid}/reprocess")]
    public async Task<IActionResult> Reprocess(Guid id, CancellationToken ct)
    {
        var detail = await _service.ReprocessFailuresAsync(id, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost("batches/{id:guid}/reprocess-all")]
    public async Task<IActionResult> ReprocessAll(Guid id, [FromQuery] Guid? jobId, CancellationToken ct)
    {
        var detail = await _service.ReprocessAllAsync(id, jobId ?? Guid.NewGuid(), ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPatch("{ticketId:guid}")]
    public async Task<IActionResult> EditTicket(Guid ticketId, [FromBody] TicketEditDto dto, CancellationToken ct)
    {
        var result = await _service.UpdateTicketAsync(ticketId, dto, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{ticketId:guid}/similar")]
    public async Task<IActionResult> SimilarTickets(Guid ticketId, CancellationToken ct)
        => Ok(await _service.GetSimilarAsync(ticketId, ct));

    [HttpDelete("similarities/{id:guid}")]
    public async Task<IActionResult> RemoveSimilarity(Guid id, CancellationToken ct)
    {
        await _service.RemoveSimilarityAsync(id, ct);
        return NoContent();
    }

    [HttpGet("batches/{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id, [FromQuery] string? columns, CancellationToken ct)
    {
        var cols = columns?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var bytes = await _service.ExportAsync(id, cols, ct);
        return bytes is null
            ? NotFound()
            : File(bytes, "text/csv", $"tickets_classified_{id:N}.csv");
    }

    [HttpGet("export/columns")]
    public IActionResult AvailableColumns()
        => Ok(CsvService.AvailableColumns.Select(c => new { key = c.Key, label = c.Label }));
}
