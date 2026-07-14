using System.Text;
using TicketClassifier.Api.Gateways.Interface;
using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Prompts;

public class ClassificacaoPromptBuilder
{
    private readonly IReadOnlyList<ParametroClassificacao> _parametros;

    public ClassificacaoPromptBuilder(IReadOnlyList<ParametroClassificacao> parametros)
    {
        _parametros = parametros;
    }

    public string ConstruirLote(IReadOnlyList<TicketParaClassificar> itens, int loteAtual = 1, int totalLotes = 1, int totalTickets = 0)
    {
        var lista = new StringBuilder();
        foreach (var t in itens)
        {
            var desc = t.Descricao.Length > 600 ? t.Descricao[..600] : t.Descricao;
            lista.Append($"[{t.Indice}] Assunto: {t.Assunto} | Descrição: {desc}\n");
        }

        var totalReal = totalTickets > 0 ? totalTickets : itens.Count;

        var sb = new StringBuilder();
        sb.Append("Você é um analista de suporte técnico experiente. Classifique CADA ticket da lista com precisão.\n\n");

        sb.Append($"CONTEXTO DO LOTE: Você está processando o lote {loteAtual} de {totalLotes} ");
        sb.Append($"(total de {totalReal} tickets no arquivo). Este lote contém {itens.Count} tickets. ");
        sb.Append("Responda TODOS os tickets deste lote — não omita nenhum.\n\n");

        sb.Append("Responda SOMENTE com um ARRAY JSON válido (sem markdown, sem ```). Um objeto por ticket, ");
        sb.Append("incluindo o campo \"indice\" IGUAL ao número entre colchetes do ticket. Use EXATAMENTE um dos ");
        sb.Append("valores permitidos, com a mesma acentuação.\n\n");

        sb.AppendLine($"categoria: {string.Join(" | ", Categorias.Lista)}");
        sb.AppendLine($"prioridade: {string.Join(" | ", Categorias.Prioridades)}");
        sb.AppendLine($"departamento: {string.Join(" | ", Categorias.Departamentos)}");
        sb.AppendLine($"sentimento: {string.Join(" | ", Categorias.Sentimentos)}");
        sb.AppendLine();

        MontarRegrasCategoria(sb);
        MontarRegrasPrioridade(sb);
        MontarRegrasSentimento(sb);
        MontarSecaoTags(sb);

        sb.Append("Campo \"confianca\" (0.0 a 1.0): indica o quão seguro você está na classificação.\n");
        sb.Append("- 0.90–1.00: ticket muito claro, palavras-chave explícitas, classificação óbvia.\n");
        sb.Append("- 0.75–0.89: boa certeza, contexto suficiente para decidir.\n");
        sb.Append("- 0.50–0.74: ambíguo, poderia ser outra categoria/prioridade.\n");
        sb.Append("- abaixo de 0.50: muito vago, informação insuficiente.\n");
        sb.Append("Varie a confiança de acordo com a clareza de CADA ticket. NÃO use o mesmo valor para todos.\n\n");

        sb.Append("Formato de cada item:\n");
        sb.Append("{\"indice\":0,\"categoria\":\"\",\"prioridade\":\"\",\"departamento\":\"\",\"resumo\":\"\",\"sentimento\":\"\",\"tags\":[],\"confianca\":0.85,\"justificativa\":\"\"}\n\n");

        sb.Append("--- TICKETS ---\n");
        sb.Append(lista);
        sb.Append("--- FIM ---");

        return sb.ToString();
    }

    private IReadOnlyList<ParametroClassificacao> Ativos(string tipo) =>
        _parametros.Where(p => p.Tipo == tipo && p.Ativo).ToList();

    private void MontarRegrasCategoria(StringBuilder sb)
    {
        var regrasCategoria = Ativos("categoria")
            .GroupBy(p => p.Alvo ?? "Outro")
            .ToDictionary(g => g.Key, g => g.Select(p => p.Termo).ToList());

        var perguntaIndicadores = Ativos("pergunta").Select(p => p.Termo).ToList();
        var reclamacaoIndicadores = Ativos("reclamacao").Select(p => p.Termo).ToList();

        sb.Append("REGRAS DE CATEGORIA (aplique com atenção — o padrão NÃO é 'Outro'):\n");

        if (perguntaIndicadores.Count > 0 || regrasCategoria.ContainsKey("Dúvida"))
        {
            var indicadores = perguntaIndicadores.Count > 0
                ? string.Join("\", \"", perguntaIndicadores.Take(8))
                : "como faço, onde fica, é possível, ?";
            sb.Append($"- Dúvida: o cliente faz uma PERGUNTA ou pede orientação. Indicadores: \"{indicadores}\". ");
            sb.Append("IMPORTANTE: se o texto é uma pergunta sobre como usar algo e NÃO relata um erro/falha, a categoria é Dúvida, não Bug.\n");
        }

        foreach (var cat in Categorias.Lista)
        {
            if (cat == "Dúvida" || cat == "Outro") continue;

            if (cat == "Reclamação" && reclamacaoIndicadores.Count > 0)
            {
                var ind = string.Join("\", \"", reclamacaoIndicadores.Take(8));
                sb.Append($"- Reclamação: o cliente expressa INSATISFAÇÃO com o serviço/atendimento. Indicadores: \"{ind}\".\n");
                continue;
            }

            if (regrasCategoria.TryGetValue(cat, out var termos) && termos.Count > 0)
            {
                var ind = string.Join("\", \"", termos.Take(8));
                sb.Append($"- {cat}: indicadores: \"{ind}\".\n");
            }
            else
            {
                sb.Append($"- {cat}.\n");
            }
        }

        sb.Append("- Outro: SOMENTE se nenhuma das categorias acima se aplica.\n\n");

        var regrasDept = Ativos("departamento");
        if (regrasDept.Count > 0)
        {
            sb.Append("REGRAS DE DEPARTAMENTO (baseado na categoria):\n");
            foreach (var r in regrasDept)
                sb.Append($"- Categoria \"{r.Termo}\" → Departamento \"{r.Alvo}\".\n");
            sb.Append("- Demais categorias → Departamento \"Suporte\".\n\n");
        }
    }

    private void MontarRegrasPrioridade(StringBuilder sb)
    {
        var regrasPrioridade = Ativos("prioridade")
            .GroupBy(p => p.Alvo ?? "Média")
            .ToDictionary(g => g.Key, g => g.Select(p => p.Termo).ToList());

        sb.Append("Critérios de PRIORIDADE (não use 'Média' por padrão — escolha de fato):\n");

        foreach (var prio in Categorias.Prioridades.Reverse())
        {
            if (regrasPrioridade.TryGetValue(prio, out var termos) && termos.Count > 0)
            {
                var ind = string.Join("\", \"", termos.Take(8));
                sb.Append($"- {prio}: indicadores: \"{ind}\".\n");
            }
            else
            {
                sb.Append($"- {prio}.\n");
            }
        }

        sb.AppendLine();
    }

    private void MontarRegrasSentimento(StringBuilder sb)
    {
        var positivos = Ativos("sentimento_positivo").Select(p => p.Termo).ToList();
        var negativos = Ativos("sentimento_negativo").Select(p => p.Termo).ToList();

        sb.Append("Campo \"sentimento\": analise o tom do texto do cliente.\n");

        if (positivos.Count > 0)
        {
            var ind = string.Join("\", \"", positivos.Take(8));
            sb.Append($"- positivo: indicadores: \"{ind}\".\n");
        }
        else
        {
            sb.Append("- positivo: elogio, satisfação, agradecimento, sugestão construtiva.\n");
        }

        if (negativos.Count > 0)
        {
            var ind = string.Join("\", \"", negativos.Take(8));
            sb.Append($"- negativo: indicadores: \"{ind}\".\n");
        }
        else
        {
            sb.Append("- negativo: reclamação, frustração, raiva, insatisfação, urgência com irritação.\n");
        }

        sb.Append("- neutro: tom informativo, dúvida objetiva, solicitação factual.\n\n");
    }

    private void MontarSecaoTags(StringBuilder sb)
    {
        var tagKeywords = Ativos("tag").Select(p => p.Termo).ToList();

        sb.Append("Campo \"tags\": array com 2 a 5 palavras-chave relevantes extraídas do ticket (em minúsculas). ");
        sb.Append("Devem representar os temas centrais do ticket.");

        if (tagKeywords.Count > 0)
        {
            var exemplos = string.Join("\", \"", tagKeywords.Take(15));
            sb.Append($" Vocabulário de referência: [\"{exemplos}\"].");
        }

        sb.Append("\n\n");
    }
}
