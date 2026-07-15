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
            lista.Append($"[{t.Indice}] Subject: {t.Assunto} | Description: {desc}\n");
        }

        var totalReal = totalTickets > 0 ? totalTickets : itens.Count;

        var sb = new StringBuilder();
        sb.Append("You are an experienced technical support analyst. Classify EACH ticket in the list with precision.\n\n");

        sb.Append($"BATCH CONTEXT: You are processing batch {loteAtual} of {totalLotes} ");
        sb.Append($"(total of {totalReal} tickets in the file). This batch contains {itens.Count} tickets. ");
        sb.Append("Respond to ALL tickets in this batch — do not omit any.\n\n");

        sb.Append("Respond ONLY with a valid JSON ARRAY (no markdown, no ```). One object per ticket, ");
        sb.Append("including the \"indice\" field EQUAL to the number in brackets of the ticket. Use EXACTLY one of the ");
        sb.Append("allowed values, matching case exactly.\n\n");

        sb.AppendLine($"categoria: {string.Join(" | ", Categorias.Lista)}");
        sb.AppendLine($"prioridade: {string.Join(" | ", Categorias.Prioridades)}");
        sb.AppendLine($"departamento: {string.Join(" | ", Categorias.Departamentos)}");
        sb.AppendLine($"sentimento: {string.Join(" | ", Categorias.Sentimentos)}");
        sb.AppendLine();

        MontarRegrasCategoria(sb);
        MontarRegrasPrioridade(sb);
        MontarRegrasSentimento(sb);
        MontarSecaoTags(sb);

        sb.Append("Field \"confianca\" (0.0 to 1.0): indicates how confident you are in the classification.\n");
        sb.Append("- 0.90–1.00: very clear ticket, explicit keywords, obvious classification.\n");
        sb.Append("- 0.75–0.89: good certainty, sufficient context to decide.\n");
        sb.Append("- 0.50–0.74: ambiguous, could be another category/priority.\n");
        sb.Append("- below 0.50: very vague, insufficient information.\n");
        sb.Append("Vary confidence according to the clarity of EACH ticket. Do NOT use the same value for all.\n\n");

        sb.Append("Format for each item:\n");
        sb.Append("{\"indice\":0,\"categoria\":\"\",\"prioridade\":\"\",\"departamento\":\"\",\"resumo\":\"\",\"sentimento\":\"\",\"tags\":[],\"confianca\":0.85,\"justificativa\":\"\"}\n\n");

        sb.Append("--- TICKETS ---\n");
        sb.Append(lista);
        sb.Append("--- END ---");

        return sb.ToString();
    }

    private IReadOnlyList<ParametroClassificacao> Ativos(string tipo) =>
        _parametros.Where(p => p.Tipo == tipo && p.Ativo).ToList();

    private void MontarRegrasCategoria(StringBuilder sb)
    {
        var regrasCategoria = Ativos("categoria")
            .GroupBy(p => p.Alvo ?? "Other")
            .ToDictionary(g => g.Key, g => g.Select(p => p.Termo).ToList());

        var perguntaIndicadores = Ativos("pergunta").Select(p => p.Termo).ToList();
        var reclamacaoIndicadores = Ativos("reclamacao").Select(p => p.Termo).ToList();

        sb.Append("CATEGORY RULES (apply carefully — the default is NOT 'Other'):\n");

        if (perguntaIndicadores.Count > 0 || regrasCategoria.ContainsKey("Question"))
        {
            var indicadores = perguntaIndicadores.Count > 0
                ? string.Join("\", \"", perguntaIndicadores.Take(8))
                : "how do I, where is, is it possible, ?";
            sb.Append($"- Question: the customer asks a QUESTION or seeks guidance. Indicators: \"{indicadores}\". ");
            sb.Append("IMPORTANT: if the text is a question about how to use something and does NOT report an error/failure, the category is Question, not Bug.\n");
        }

        foreach (var cat in Categorias.Lista)
        {
            if (cat == "Question" || cat == "Other") continue;

            if (cat == "Complaint" && reclamacaoIndicadores.Count > 0)
            {
                var ind = string.Join("\", \"", reclamacaoIndicadores.Take(8));
                sb.Append($"- Complaint: the customer expresses DISSATISFACTION with the service/support. Indicators: \"{ind}\".\n");
                continue;
            }

            if (regrasCategoria.TryGetValue(cat, out var termos) && termos.Count > 0)
            {
                var ind = string.Join("\", \"", termos.Take(8));
                sb.Append($"- {cat}: indicators: \"{ind}\".\n");
            }
            else
            {
                sb.Append($"- {cat}.\n");
            }
        }

        sb.Append("- Other: ONLY if none of the above categories apply.\n\n");

        var regrasDept = Ativos("departamento");
        if (regrasDept.Count > 0)
        {
            sb.Append("DEPARTMENT RULES (based on category):\n");
            foreach (var r in regrasDept)
                sb.Append($"- Category \"{r.Termo}\" → Department \"{r.Alvo}\".\n");
            sb.Append("- Remaining categories → Department \"Support\".\n\n");
        }
    }

    private void MontarRegrasPrioridade(StringBuilder sb)
    {
        var regrasPrioridade = Ativos("prioridade")
            .GroupBy(p => p.Alvo ?? "Medium")
            .ToDictionary(g => g.Key, g => g.Select(p => p.Termo).ToList());

        sb.Append("PRIORITY criteria (do not default to 'Medium' — make an actual choice):\n");

        foreach (var prio in Categorias.Prioridades.Reverse())
        {
            if (regrasPrioridade.TryGetValue(prio, out var termos) && termos.Count > 0)
            {
                var ind = string.Join("\", \"", termos.Take(8));
                sb.Append($"- {prio}: indicators: \"{ind}\".\n");
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

        sb.Append("Field \"sentimento\": analyze the tone of the customer's text.\n");

        if (positivos.Count > 0)
        {
            var ind = string.Join("\", \"", positivos.Take(8));
            sb.Append($"- positive: indicators: \"{ind}\".\n");
        }
        else
        {
            sb.Append("- positive: praise, satisfaction, gratitude, constructive suggestion.\n");
        }

        if (negativos.Count > 0)
        {
            var ind = string.Join("\", \"", negativos.Take(8));
            sb.Append($"- negative: indicators: \"{ind}\".\n");
        }
        else
        {
            sb.Append("- negative: complaint, frustration, anger, dissatisfaction, urgency with irritation.\n");
        }

        sb.Append("- neutral: informative tone, objective question, factual request.\n\n");
    }

    private void MontarSecaoTags(StringBuilder sb)
    {
        var tagKeywords = Ativos("tag").Select(p => p.Termo).ToList();

        sb.Append("Field \"tags\": array with 2 to 5 relevant keywords extracted from the ticket (lowercase). ");
        sb.Append("Should represent the central themes of the ticket.");

        if (tagKeywords.Count > 0)
        {
            var exemplos = string.Join("\", \"", tagKeywords.Take(15));
            sb.Append($" Reference vocabulary: [\"{exemplos}\"].");
        }

        sb.Append("\n\n");
    }
}
