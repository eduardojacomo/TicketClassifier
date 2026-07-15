using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Data;

public static class ParametroSeed
{
    public static List<ParametroClassificacao> Gerar()
    {
        var lista = new List<ParametroClassificacao>();

        void Add(string tipo, string termo, string? alvo = null)
            => lista.Add(new() { Tipo = tipo, Termo = termo, Alvo = alvo });

        // ── Category rules (term → target category) ─────────────────
        foreach (var t in new[] { "login", "password", "access", "log in", "authentication", "sign in", "can't access" })
            Add("categoria", t, "Login");

        foreach (var t in new[] { "payment", "card", "invoice slip", "pix", "billing", "transaction", "undue", "charged" })
            Add("categoria", t, "Payment");

        foreach (var t in new[] { "invoice", "refund", "subscription", "receipt", "financial", "plan cancellation" })
            Add("categoria", t, "Financial");

        foreach (var t in new[] { "slow", "freezing", "takes long", "performance", "sluggish", "timeout" })
            Add("categoria", t, "Performance");

        foreach (var t in new[] { "integration", "api", "webhook", "synchro" })
            Add("categoria", t, "Integration");

        foreach (var t in new[] { "error", "bug", "failure", "broke", "not working", "crash", "freeze", "closes by itself", "froze", "won't open", "won't load", "won't save" })
            Add("categoria", t, "Bug");

        foreach (var t in new[] { "registration", "register", "update info", "address", "ownership" })
            Add("categoria", t, "Registration");

        foreach (var t in new[] { "sales", "partnership", "selling", "quote", "hire", "proposal" })
            Add("categoria", t, "Sales");

        foreach (var t in new[] { "suggestion", "I wish", "could you add", "feature", "feature", "improvement", "it would be nice" })
            Add("categoria", t, "Suggestion");

        foreach (var t in new[] { "praise", "congratulations", "excellent", "10 out of 10", "recommend", "loved it", "wonderf", "fantast" })
            Add("categoria", t, "Praise");

        // ── Priority rules ─────────────────────────────────────────
        foreach (var t in new[] { "urgent", "immediate", "stopped", "production", "critical", "can't work", "immediate solution" })
            Add("prioridade", t, "Critical");

        foreach (var t in new[] { "important", "fast", "as soon as possible", "blocked", "I'm blocked", "need a solution", "I can't", "prevents" })
            Add("prioridade", t, "High");

        foreach (var t in new[] { "when you can", "no rush", "suggestion", "it would be nice", "not urgent" })
            Add("prioridade", t, "Low");

        // ── Department rules (category → department) ────────────
        foreach (var cat in new[] { "Bug", "Performance", "Integration" })
            Add("departamento", cat, "Development");
        foreach (var cat in new[] { "Payment", "Financial" })
            Add("departamento", cat, "Financial");
        Add("departamento", "Sales", "Sales");
        foreach (var cat in new[] { "Suggestion", "Praise" })
            Add("departamento", cat, "Product");

        // ── Question indicators ──────────────────────────────────
        foreach (var t in new[] {
            "how do I", "how can I", "how do I", "how to", "how does it work",
            "where is", "where is it", "where do I find", "where can I find",
            "is it possible", "can I", "is there a way", "can you",
            "what is", "what does it mean", "which is", "what is the",
            "why", "why is", "?",
            "I would like to know", "I need to know", "explain to me", "help me",
            "can someone", "does someone know", "could you", "can you",
            "I don't know how", "I can't find", "I can't find it", "I can't locate",
            "I have a question", "I have a doubt", "question about" })
            Add("pergunta", t);

        // ── Complaint indicators ────────────────────────────────
        foreach (var t in new[] {
            "terrible", "horrible", "absurd", "unacceptable", "dissatisf",
            "frustr", "outrag", "neglect", "lack of respect",
            "nobody solves", "nobody helps", "nobody answers",
            "doesn't solve", "don't solve", "not resolved",
            "already complained", "already contacted", "already called",
            "worst", "disappoint", "outrag", "deplorable", "shameful",
            "very dissatisfied", "totally dissatisfied",
            "bad service", "terrible service", "poorly served",
            "going to cancel", "going to sue", "consumer protection", "complaint board" })
            Add("reclamacao", t);

        // ── Positive sentiment ──────────────────────────────────
        foreach (var t in new[] {
            "thank you", "excellent", "congratulations", "great", "satisf", "grateful",
            "praise", "wonderf", "fantast", "very good", "loved it", "loved",
            "10 out of 10", "recommend", "top" })
            Add("sentimento_positivo", t);

        // ── Negative sentiment ──────────────────────────────────
        foreach (var t in new[] {
            "terrible", "horrible", "absurd", "anger", "neglect", "unacceptable",
            "complaint", "dissatisf", "frustr", "outrag", "outrag", "disappoint",
            "deplorable", "shameful", "nobody solves", "worst" })
            Add("sentimento_negativo", t);

        // ── Tag keywords ─────────────────────────────────────
        foreach (var t in new[] {
            "login", "password", "access", "authentication",
            "payment", "card", "invoice slip", "pix", "billing", "invoice", "refund", "undue",
            "subscription", "cancellation", "plan",
            "error", "bug", "failure", "crash", "freeze", "closes by itself",
            "slow", "performance", "timeout", "takes long",
            "integration", "api", "webhook",
            "registration", "register", "invite", "user",
            "receipt", "report", "export", "pdf",
            "suggestion", "feature", "improvement",
            "sales", "selling", "quote",
            "service", "support", "response",
            "question", "how to", "tutorial" })
            Add("tag", t);

        return lista;
    }
}
