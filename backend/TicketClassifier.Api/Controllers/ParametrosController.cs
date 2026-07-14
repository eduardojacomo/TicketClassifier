using Microsoft.AspNetCore.Mvc;
using TicketClassifier.Api.Models;
using TicketClassifier.Api.Services.Interface;

namespace TicketClassifier.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParametrosController : ControllerBase
{
    private readonly ITicketService _service;

    public ParametrosController(ITicketService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? tipo, CancellationToken ct)
        => Ok(await _service.ListarParametrosAsync(tipo, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct)
    {
        var p = await _service.ObterParametroAsync(id, ct);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] ParametroClassificacao dto, CancellationToken ct)
    {
        dto.Id = Guid.NewGuid();
        var criado = await _service.CriarParametroAsync(dto, ct);
        return CreatedAtAction(nameof(Obter), new { id = criado.Id }, criado);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] ParametroClassificacao dto, CancellationToken ct)
    {
        var atualizado = await _service.AtualizarParametroAsync(id, dto, ct);
        return atualizado is null ? NotFound() : Ok(atualizado);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remover(Guid id, CancellationToken ct)
    {
        await _service.RemoverParametroAsync(id, ct);
        return NoContent();
    }

    [HttpGet("tipos")]
    public IActionResult Tipos()
        => Ok(new[] { "categoria", "prioridade", "departamento", "pergunta", "reclamacao", "sentimento_positivo", "sentimento_negativo", "tag" });
}
