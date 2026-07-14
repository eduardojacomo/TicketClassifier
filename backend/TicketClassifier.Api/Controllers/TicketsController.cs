using Microsoft.AspNetCore.Mvc;
using TicketClassifier.Api.Services;
using TicketClassifier.Api.Services.Interface;

namespace TicketClassifier.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _service;
    private readonly IProgressoStore _progresso;

    public TicketsController(ITicketService service, IProgressoStore progresso)
    {
        _service = service;
        _progresso = progresso;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] Guid? jobId, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { erro = "Nenhum arquivo enviado." });

        try
        {
            using var stream = file.OpenReadStream();
            var resumo = await _service.ProcessarCsvAsync(stream, file.FileName, jobId ?? Guid.NewGuid(), ct);
            return Ok(resumo);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    /// <summary>Progresso do processamento de um upload (para a barra/contador).</summary>
    [HttpGet("progress/{jobId:guid}")]
    public IActionResult Progress(Guid jobId)
    {
        var p = _progresso.Obter(jobId);
        return Ok(new
        {
            total = p?.Total ?? 0,
            processados = p?.Processados ?? 0,
            ok = p?.Ok ?? 0,
            falhas = p?.Falhas ?? 0,
            totalLotes = p?.TotalLotes ?? 0,
            lotesConcluidos = p?.LotesConcluidos ?? 0,
            concluido = p?.Concluido ?? false,
            batchId = p?.BatchId,
        });
    }

    [HttpGet("batches")]
    public async Task<IActionResult> Batches(CancellationToken ct)
        => Ok(await _service.ListarAsync(ct));

    [HttpGet("batches/{id:guid}")]
    public async Task<IActionResult> Batch(Guid id, CancellationToken ct)
    {
        var detalhe = await _service.ObterAsync(id, ct);
        return detalhe is null ? NotFound() : Ok(detalhe);
    }

    /// <summary>Reprocessa apenas os tickets que falharam (fallback) no lote.</summary>
    [HttpPost("batches/{id:guid}/reprocessar")]
    public async Task<IActionResult> Reprocessar(Guid id, CancellationToken ct)
    {
        var detalhe = await _service.ReprocessarFalhasAsync(id, ct);
        return detalhe is null ? NotFound() : Ok(detalhe);
    }

    [HttpGet("batches/{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id, CancellationToken ct)
    {
        var bytes = await _service.ExportarAsync(id, ct);
        return bytes is null
            ? NotFound()
            : File(bytes, "text/csv", $"tickets_classificados_{id:N}.csv");
    }
}
