using Microsoft.AspNetCore.Mvc;
using TicketClassifier.Api.Models;
using TicketClassifier.Api.Services.Interface;

namespace TicketClassifier.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParametersController : ControllerBase
{
    private readonly ITicketService _service;

    public ParametersController(ITicketService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? type, CancellationToken ct)
        => Ok(await _service.ListParametersAsync(type, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var p = await _service.GetParameterAsync(id, ct);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClassificationParameter dto, CancellationToken ct)
    {
        dto.Id = Guid.NewGuid();
        var created = await _service.CreateParameterAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ClassificationParameter dto, CancellationToken ct)
    {
        var updated = await _service.UpdateParameterAsync(id, dto, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.RemoveParameterAsync(id, ct);
        return NoContent();
    }

    [HttpGet("types")]
    public IActionResult Types()
        => Ok(new[] { "categoria", "prioridade", "departamento", "pergunta", "reclamacao", "sentimento_positivo", "sentimento_negativo", "tag" });
}
