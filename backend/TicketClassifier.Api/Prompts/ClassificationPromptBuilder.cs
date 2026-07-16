using System.Text;
using TicketClassifier.Api.Gateways.Interface;
using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Prompts;

public class ClassificationPromptBuilder
{
    private readonly IReadOnlyList<ClassificationParameter> _parameters;

    public ClassificationPromptBuilder(IReadOnlyList<ClassificationParameter> parameters)
    {
        _parameters = parameters;
    }

    public string BuildBatch(IReadOnlyList<TicketToClassify> items, int currentBatch = 1, int totalBatches = 1, int totalTickets = 0)
    {
        var list = new StringBuilder();
        foreach (var t in items)
        {
            var desc = t.Description.Length > 600 ? t.Description[..600] : t.Description;
            list.Append($"[{t.Index}] Subject: {t.Subject} | Description: {desc}\n");
        }

        var actualTotal = totalTickets > 0 ? totalTickets : items.Count;

        var sb = new StringBuilder();
        sb.Append("You are an experienced technical support analyst. Classify EACH ticket in the list with precision.\n\n");

        sb.Append($"BATCH CONTEXT: You are processing batch {currentBatch} of {totalBatches} ");
        sb.Append($"(total of {actualTotal} tickets in the file). This batch contains {items.Count} tickets. ");
        sb.Append("Respond to ALL tickets in this batch — do not omit any.\n\n");

        sb.Append("Respond ONLY with a valid JSON ARRAY (no markdown, no ```). One object per ticket, ");
        sb.Append("including the \"indice\" field EQUAL to the number in brackets of the ticket. Use EXACTLY one of the ");
        sb.Append("allowed values, matching case exactly.\n\n");

        sb.AppendLine($"categoria: {string.Join(" | ", Categories.CategoryList)}");
        sb.AppendLine($"prioridade: {string.Join(" | ", Categories.PriorityList)}");
        sb.AppendLine($"departamento: {string.Join(" | ", Categories.DepartmentList)}");
        sb.AppendLine($"sentimento: {string.Join(" | ", Categories.SentimentList)}");
        sb.AppendLine();

        BuildCategoryRules(sb);
        BuildPriorityRules(sb);
        BuildSentimentRules(sb);
        BuildTagsSection(sb);

        sb.Append("Field \"confianca\" (0.0 to 1.0): indicates how confident you are in the classification.\n");
        sb.Append("- 0.90–1.00: very clear ticket, explicit keywords, obvious classification.\n");
        sb.Append("- 0.75–0.89: good certainty, sufficient context to decide.\n");
        sb.Append("- 0.50–0.74: ambiguous, could be another category/priority.\n");
        sb.Append("- below 0.50: very vague, insufficient information.\n");
        sb.Append("Vary confidence according to the clarity of EACH ticket. Do NOT use the same value for all.\n\n");

        sb.Append("Format for each item:\n");
        sb.Append("{\"indice\":0,\"categoria\":\"\",\"prioridade\":\"\",\"departamento\":\"\",\"resumo\":\"\",\"sentimento\":\"\",\"tags\":[],\"confianca\":0.85,\"justificativa\":\"\"}\n\n");

        sb.Append("--- TICKETS ---\n");
        sb.Append(list);
        sb.Append("--- END ---");

        return sb.ToString();
    }

    private IReadOnlyList<ClassificationParameter> Active(string type) =>
        _parameters.Where(p => p.Type == type && p.Active).ToList();

    private void BuildCategoryRules(StringBuilder sb)
    {
        var categoryRules = Active("categoria")
            .GroupBy(p => p.Target ?? "Other")
            .ToDictionary(g => g.Key, g => g.Select(p => p.Term).ToList());

        var questionIndicators = Active("pergunta").Select(p => p.Term).ToList();
        var complaintIndicators = Active("reclamacao").Select(p => p.Term).ToList();

        sb.Append("CATEGORY RULES (apply carefully — the default is NOT 'Other'):\n");

        if (questionIndicators.Count > 0 || categoryRules.ContainsKey("Question"))
        {
            var indicators = questionIndicators.Count > 0
                ? string.Join("\", \"", questionIndicators.Take(8))
                : "how do I, where is, is it possible, ?";
            sb.Append($"- Question: the customer asks a QUESTION or seeks guidance. Indicators: \"{indicators}\". ");
            sb.Append("IMPORTANT: if the text is a question about how to use something and does NOT report an error/failure, the category is Question, not Bug.\n");
        }

        foreach (var cat in Categories.CategoryList)
        {
            if (cat == "Question" || cat == "Other") continue;

            if (cat == "Complaint" && complaintIndicators.Count > 0)
            {
                var ind = string.Join("\", \"", complaintIndicators.Take(8));
                sb.Append($"- Complaint: the customer expresses DISSATISFACTION with the service/support. Indicators: \"{ind}\".\n");
                continue;
            }

            if (categoryRules.TryGetValue(cat, out var terms) && terms.Count > 0)
            {
                var ind = string.Join("\", \"", terms.Take(8));
                sb.Append($"- {cat}: indicators: \"{ind}\".\n");
            }
            else
            {
                sb.Append($"- {cat}.\n");
            }
        }

        sb.Append("- Other: ONLY if none of the above categories apply.\n\n");

        var departmentRules = Active("departamento");
        if (departmentRules.Count > 0)
        {
            sb.Append("DEPARTMENT RULES (based on category):\n");
            foreach (var r in departmentRules)
                sb.Append($"- Category \"{r.Term}\" → Department \"{r.Target}\".\n");
            sb.Append("- Remaining categories → Department \"Support\".\n\n");
        }
    }

    private void BuildPriorityRules(StringBuilder sb)
    {
        var priorityRules = Active("prioridade")
            .GroupBy(p => p.Target ?? "Medium")
            .ToDictionary(g => g.Key, g => g.Select(p => p.Term).ToList());

        sb.Append("PRIORITY criteria (do not default to 'Medium' — make an actual choice):\n");

        foreach (var prio in Categories.PriorityList.Reverse())
        {
            if (priorityRules.TryGetValue(prio, out var terms) && terms.Count > 0)
            {
                var ind = string.Join("\", \"", terms.Take(8));
                sb.Append($"- {prio}: indicators: \"{ind}\".\n");
            }
            else
            {
                sb.Append($"- {prio}.\n");
            }
        }

        sb.AppendLine();
    }

    private void BuildSentimentRules(StringBuilder sb)
    {
        var positiveIndicators = Active("sentimento_positivo").Select(p => p.Term).ToList();
        var negativeIndicators = Active("sentimento_negativo").Select(p => p.Term).ToList();

        sb.Append("Field \"sentimento\": analyze the tone of the customer's text.\n");

        if (positiveIndicators.Count > 0)
        {
            var ind = string.Join("\", \"", positiveIndicators.Take(8));
            sb.Append($"- positive: indicators: \"{ind}\".\n");
        }
        else
        {
            sb.Append("- positive: praise, satisfaction, gratitude, constructive suggestion.\n");
        }

        if (negativeIndicators.Count > 0)
        {
            var ind = string.Join("\", \"", negativeIndicators.Take(8));
            sb.Append($"- negative: indicators: \"{ind}\".\n");
        }
        else
        {
            sb.Append("- negative: complaint, frustration, anger, dissatisfaction, urgency with irritation.\n");
        }

        sb.Append("- neutral: informative tone, objective question, factual request.\n\n");
    }

    private void BuildTagsSection(StringBuilder sb)
    {
        var tagKeywords = Active("tag").Select(p => p.Term).ToList();

        sb.Append("Field \"tags\": array with 2 to 5 relevant keywords extracted from the ticket (lowercase). ");
        sb.Append("Should represent the central themes of the ticket.");

        if (tagKeywords.Count > 0)
        {
            var examples = string.Join("\", \"", tagKeywords.Take(15));
            sb.Append($" Reference vocabulary: [\"{examples}\"].");
        }

        sb.Append("\n\n");
    }
}
