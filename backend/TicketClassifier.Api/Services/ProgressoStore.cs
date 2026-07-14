using System.Collections.Concurrent;

namespace TicketClassifier.Api.Services;

public class Progresso
{
    public int Total { get; set; }
    public int Processados { get; set; }
    public int Ok { get; set; }
    public int Falhas { get; set; }
    public int TotalLotes { get; set; }
    public int LotesConcluidos { get; set; }
    public bool Concluido { get; set; }
    public Guid? BatchId { get; set; }
    public DateTime Inicio { get; set; } = DateTime.UtcNow;
}

public interface IProgressoStore
{
    void Iniciar(Guid jobId, int total, int totalLotes);
    void ReportarLote(Guid jobId, int processados, int ok);
    void Concluir(Guid jobId, Guid batchId);
    Progresso? Obter(Guid jobId);
}

/// <summary>
/// Rastreia o progresso do processamento por jobId (em memória). O frontend
/// consulta durante o upload para exibir contadores, barra e log.
/// </summary>
public class ProgressoStore : IProgressoStore
{
    private readonly ConcurrentDictionary<Guid, Progresso> _map = new();

    public void Iniciar(Guid jobId, int total, int totalLotes)
        => _map[jobId] = new Progresso { Total = total, TotalLotes = totalLotes };

    public void ReportarLote(Guid jobId, int processados, int ok)
    {
        if (_map.TryGetValue(jobId, out var p))
            lock (p)
            {
                p.Processados += processados;
                p.Ok += ok;
                p.Falhas += processados - ok;
                p.LotesConcluidos++;
            }
    }

    public void Concluir(Guid jobId, Guid batchId)
    {
        if (_map.TryGetValue(jobId, out var p))
            lock (p)
            {
                p.Processados = p.Total;
                p.Concluido = true;
                p.BatchId = batchId;
            }
        // Limpeza tardia para o frontend ler o estado final.
        _ = Task.Delay(TimeSpan.FromMinutes(2)).ContinueWith(t => _map.TryRemove(jobId, out _));
    }

    public Progresso? Obter(Guid jobId) => _map.GetValueOrDefault(jobId);
}
