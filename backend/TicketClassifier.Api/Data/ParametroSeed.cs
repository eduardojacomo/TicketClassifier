using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Data;

public static class ParametroSeed
{
    public static List<ParametroClassificacao> Gerar()
    {
        var lista = new List<ParametroClassificacao>();

        void Add(string tipo, string termo, string? alvo = null)
            => lista.Add(new() { Tipo = tipo, Termo = termo, Alvo = alvo });

        // ── Regras de categoria (termo → categoria alvo) ─────────────────
        foreach (var t in new[] { "login", "senha", "acesso", "logar", "autenticação", "entrar", "não consigo acessar" })
            Add("categoria", t, "Login");

        foreach (var t in new[] { "pagamento", "cartão", "boleto", "pix", "cobrança", "transação", "indevida", "cobrado" })
            Add("categoria", t, "Pagamento");

        foreach (var t in new[] { "fatura", "reembolso", "assinatura", "nota fiscal", "financeiro", "cancelamento de plano" })
            Add("categoria", t, "Financeiro");

        foreach (var t in new[] { "lento", "travando", "demora", "performance", "lentidão", "timeout" })
            Add("categoria", t, "Performance");

        foreach (var t in new[] { "integração", "api", "webhook", "sincroniz" })
            Add("categoria", t, "Integração");

        foreach (var t in new[] { "erro", "bug", "falha", "quebrou", "não funciona", "crash", "trava", "fecha sozinho", "travou", "não abre", "não carrega", "não salva" })
            Add("categoria", t, "Bug");

        foreach (var t in new[] { "cadastro", "registrar", "atualizar dados", "endereço", "titularidade" })
            Add("categoria", t, "Cadastro");

        foreach (var t in new[] { "comercial", "parceria", "vendas", "orçamento", "contratar", "proposta" })
            Add("categoria", t, "Comercial");

        foreach (var t in new[] { "sugestão", "gostaria que", "poderia adicionar", "recurso", "feature", "melhoria", "seria bom" })
            Add("categoria", t, "Sugestão");

        foreach (var t in new[] { "elogio", "parabéns", "excelente", "nota 10", "recomendo", "adorei", "maravilh", "fantástic" })
            Add("categoria", t, "Elogio");

        // ── Regras de prioridade ─────────────────────────────────────────
        foreach (var t in new[] { "urgente", "imediato", "parado", "produção", "crítico", "não consigo trabalhar", "solução imediata" })
            Add("prioridade", t, "Crítica");

        foreach (var t in new[] { "importante", "rápido", "o quanto antes", "bloqueado", "estou bloqueado", "preciso de solução", "não consigo", "impede" })
            Add("prioridade", t, "Alta");

        foreach (var t in new[] { "quando puder", "sem pressa", "sugestão", "seria bom", "não urgente" })
            Add("prioridade", t, "Baixa");

        // ── Regras de departamento (categoria → departamento) ────────────
        foreach (var cat in new[] { "Bug", "Performance", "Integração" })
            Add("departamento", cat, "Desenvolvimento");
        foreach (var cat in new[] { "Pagamento", "Financeiro" })
            Add("departamento", cat, "Financeiro");
        Add("departamento", "Comercial", "Comercial");
        foreach (var cat in new[] { "Sugestão", "Elogio" })
            Add("departamento", cat, "Produto");

        // ── Indicadores de pergunta ──────────────────────────────────────
        foreach (var t in new[] {
            "como faço", "como posso", "como eu", "como fazer", "como funciona",
            "onde fica", "onde está", "onde encontro", "onde acho",
            "é possível", "consigo", "tem como", "dá pra", "da pra",
            "o que é", "o que significa", "qual é", "qual a",
            "por que", "porque", "?",
            "gostaria de saber", "preciso saber", "me explica", "me ajuda",
            "alguém pode", "alguém sabe", "poderia me", "pode me",
            "não sei como", "não encontro", "não acho", "não consigo encontrar",
            "estou com dúvida", "tenho dúvida", "dúvida sobre" })
            Add("pergunta", t);

        // ── Indicadores de reclamação ────────────────────────────────────
        foreach (var t in new[] {
            "péssimo", "horrível", "absurdo", "inaceitável", "insatisf",
            "frustr", "revoltad", "descaso", "falta de respeito",
            "ninguém resolve", "ninguém ajuda", "ninguém responde",
            "não resolve", "não resolvem", "não resolvido",
            "já reclamei", "já entrei em contato", "já liguei",
            "pior", "decepcion", "indign", "lamentável", "vergonha",
            "muito insatisfeito", "totalmente insatisfeito",
            "atendimento ruim", "péssimo atendimento", "mal atendido",
            "vou cancelar", "vou processar", "procon", "reclameaqui" })
            Add("reclamacao", t);

        // ── Sentimento positivo ──────────────────────────────────────────
        foreach (var t in new[] {
            "obrigado", "excelente", "parabéns", "ótimo", "satisf", "grato",
            "elogio", "maravilh", "fantástic", "muito bom", "adorei", "amei",
            "nota 10", "recomendo", "top" })
            Add("sentimento_positivo", t);

        // ── Sentimento negativo ──────────────────────────────────────────
        foreach (var t in new[] {
            "péssimo", "horrível", "absurdo", "raiva", "descaso", "inaceitável",
            "reclamação", "insatisf", "frustr", "revoltad", "indign", "decepcion",
            "lamentável", "vergonha", "ninguém resolve", "pior" })
            Add("sentimento_negativo", t);

        // ── Palavras-chave para tags ─────────────────────────────────────
        foreach (var t in new[] {
            "login", "senha", "acesso", "autenticação",
            "pagamento", "cartão", "boleto", "pix", "cobrança", "fatura", "reembolso", "indevida",
            "assinatura", "cancelamento", "plano",
            "erro", "bug", "falha", "crash", "trava", "fecha sozinho",
            "lento", "performance", "timeout", "demora",
            "integração", "api", "webhook",
            "cadastro", "registro", "convite", "usuário",
            "nota fiscal", "relatório", "exportar", "pdf",
            "sugestão", "recurso", "melhoria",
            "comercial", "vendas", "orçamento",
            "atendimento", "suporte", "resposta",
            "dúvida", "como fazer", "tutorial" })
            Add("tag", t);

        return lista;
    }
}
