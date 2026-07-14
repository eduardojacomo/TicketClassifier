namespace TicketClassifier.Api.Dtos.Output;

public class TicketDto
{
    public Guid Id { get; set; }
    public string? ExternalId { get; set; }
    public string? Assunto { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Resumo { get; set; } = string.Empty;
    public double Confianca { get; set; }
    public string Justificativa { get; set; } = string.Empty;
    public bool ProcessadoOk { get; set; }
}

public class EstatisticasDto
{
    public int Total { get; set; }
    public int Falhas { get; set; }
    public Dictionary<string, int> PorCategoria { get; set; } = new();
    public Dictionary<string, int> PorPrioridade { get; set; } = new();
    public Dictionary<string, int> PorDepartamento { get; set; } = new();
    public double ConfiancaMedia { get; set; }
}

/// <summary>Resumo do lote (listagem e retorno do upload).</summary>
public class BatchResumoDto
{
    public Guid BatchId { get; set; }
    public string NomeArquivo { get; set; } = string.Empty;
    public int Total { get; set; }
    public DateTime DataCriacao { get; set; }
    public EstatisticasDto? Estatisticas { get; set; }
}

/// <summary>Detalhe do lote: resumo + tickets.</summary>
public class BatchDetalheDto
{
    public Guid BatchId { get; set; }
    public string NomeArquivo { get; set; } = string.Empty;
    public int Total { get; set; }
    public DateTime DataCriacao { get; set; }
    public EstatisticasDto Estatisticas { get; set; } = new();
    public List<TicketDto> Tickets { get; set; } = new();
}
