using System.Text;
using TicketClassifier.Api.Gateways.Interface;

namespace TicketClassifier.Api.Prompts;

/// <summary>
/// Constrói o prompt de classificação de tickets.
/// Centralizado aqui para que TODOS os provedores (Gemini, Anthropic, etc.)
/// usem o mesmo texto, garantindo consistência independente da IA utilizada.
/// </summary>
public static class ClassificacaoPromptBuilder
{
    /// <summary>Prompt para classificar um LOTE de tickets — a resposta é um array JSON.</summary>
    public static string ConstruirLote(IReadOnlyList<TicketParaClassificar> itens)
    {
        var lista = new StringBuilder();
        foreach (var t in itens)
        {
            var desc = t.Descricao.Length > 600 ? t.Descricao[..600] : t.Descricao;
            lista.Append($"[{t.Indice}] Assunto: {t.Assunto} | Descrição: {desc}\n");
        }

        return
            "Você é um analista de suporte técnico. Classifique CADA ticket da lista com precisão.\n\n" +
            "Responda SOMENTE com um ARRAY JSON válido (sem markdown, sem ```). Um objeto por ticket, " +
            "incluindo o campo \"indice\" IGUAL ao número entre colchetes do ticket. Use EXATAMENTE um dos " +
            "valores permitidos, com a mesma acentuação.\n\n" +
            $"categoria: {string.Join(" | ", Categorias.Lista)}\n" +
            $"prioridade: {string.Join(" | ", Categorias.Prioridades)}\n" +
            $"departamento: {string.Join(" | ", Categorias.Departamentos)}\n\n" +
            "Critérios de PRIORIDADE (não use 'Média' por padrão — escolha de fato):\n" +
            "- Crítica: sistema/serviço parado, produção afetada, perda de dados, urgência explícita.\n" +
            "- Alta: bloqueia o trabalho do cliente, erro grave, cobrança indevida, precisa de solução rápida.\n" +
            "- Média: problema comum sem bloqueio total, dúvida relevante.\n" +
            "- Baixa: dúvida simples, sugestão, sem urgência.\n\n" +
            "Campo \"confianca\" (0.0 a 1.0): indica o quão seguro você está na classificação.\n" +
            "- 0.90–1.00: ticket muito claro, palavras-chave explícitas, classificação óbvia.\n" +
            "- 0.75–0.89: boa certeza, contexto suficiente para decidir.\n" +
            "- 0.50–0.74: ambíguo, poderia ser outra categoria/prioridade.\n" +
            "- abaixo de 0.50: muito vago, informação insuficiente.\n" +
            "Varie a confiança de acordo com a clareza de CADA ticket. NÃO use o mesmo valor para todos.\n\n" +
            "Formato de cada item:\n" +
            "{\"indice\":0,\"categoria\":\"\",\"prioridade\":\"\",\"departamento\":\"\",\"resumo\":\"\",\"confianca\":0.85,\"justificativa\":\"\"}\n\n" +
            "--- TICKETS ---\n" + lista.ToString() + "--- FIM ---";
    }
}
