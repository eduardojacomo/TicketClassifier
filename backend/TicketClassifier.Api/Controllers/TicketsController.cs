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
    private readonly IProgressoStore _progresso;

    public TicketsController(ITicketService service, IProgressoStore progresso)
    {
        _service = service;
        _progresso = progresso;
    }

    [HttpGet("verificar-duplicata")]
    public async Task<IActionResult> VerificarDuplicata([FromQuery] string nomeArquivo, CancellationToken ct)
    {
        var existente = await _service.VerificarDuplicataAsync(nomeArquivo, ct);
        return Ok(new { duplicado = existente is not null, batch = existente });
    }

    [HttpPost("upload")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] Guid? jobId, [FromForm] bool sobrescrever, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { erro = "Nenhum arquivo enviado." });

        try
        {
            using var stream = file.OpenReadStream();
            var resumo = await _service.ProcessarCsvAsync(stream, file.FileName, jobId ?? Guid.NewGuid(), sobrescrever, ct);
            return Ok(resumo);
        }
        catch (CsvValidationException ex)
        {
            return BadRequest(new { erro = ex.Message, codigo = ex.Codigo });
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

    [HttpPost("batches/{id:guid}/reprocessar")]
    public async Task<IActionResult> Reprocessar(Guid id, CancellationToken ct)
    {
        var detalhe = await _service.ReprocessarFalhasAsync(id, ct);
        return detalhe is null ? NotFound() : Ok(detalhe);
    }

    [HttpPost("batches/{id:guid}/reprocessar-tudo")]
    public async Task<IActionResult> ReprocessarTudo(Guid id, [FromQuery] Guid? jobId, CancellationToken ct)
    {
        var detalhe = await _service.ReprocessarTudoAsync(id, jobId ?? Guid.NewGuid(), ct);
        return detalhe is null ? NotFound() : Ok(detalhe);
    }

    [HttpPatch("{ticketId:guid}")]
    public async Task<IActionResult> EditarTicket(Guid ticketId, [FromBody] TicketEditDto dto, CancellationToken ct)
    {
        var result = await _service.AtualizarTicketAsync(ticketId, dto, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{ticketId:guid}/similares")]
    public async Task<IActionResult> Similares(Guid ticketId, CancellationToken ct)
        => Ok(await _service.ObterSimilaresAsync(ticketId, ct));

    [HttpDelete("similaridades/{id:guid}")]
    public async Task<IActionResult> RemoverSimilaridade(Guid id, CancellationToken ct)
    {
        await _service.RemoverSimilaridadeAsync(id, ct);
        return NoContent();
    }

    [HttpGet("batches/{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id, [FromQuery] string? colunas, CancellationToken ct)
    {
        var cols = colunas?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var bytes = await _service.ExportarAsync(id, cols, ct);
        return bytes is null
            ? NotFound()
            : File(bytes, "text/csv", $"tickets_classificados_{id:N}.csv");
    }

    [HttpGet("export/colunas")]
    public IActionResult ColunasDisponiveis()
        => Ok(CsvService.ColunasDisponiveis.Select(c => new { key = c.Key, label = c.Label }));
}
