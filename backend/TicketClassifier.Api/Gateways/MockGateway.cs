using TicketClassifier.Api.Gateways.Interface;

namespace TicketClassifier.Api.Gateways;

/// <summary>
/// Estratégia heurística (palavras-chave). Permite rodar sem chave de IA.
/// </summary>
public class MockGateway : IClassificacaoGateway
{
    public string Nome => "mock";

    public Task<IReadOnlyList<ClassificacaoResultado>> ClassificarLoteAsync(
        IReadOnlyList<TicketParaClassificar> itens, CancellationToken ct = default)
    {
        IReadOnlyList<ClassificacaoResultado> resultados =
            itens.Select(t => Classificar(t.Assunto, t.Descricao)).ToList();
        return Task.FromResult(resultados);
    }

    private static ClassificacaoResultado Classificar(string? assunto, string descricao)
    {
        var texto = $"{assunto} {descricao}".ToLowerInvariant();

        var (categoria, hitsCat) = ClassificarCategoria(texto);
        var (prioridade, hitsPri) = ClassificarPrioridade(texto);

        var departamento = categoria switch
        {
            "Bug" or "Performance" or "Integração" => "Desenvolvimento",
            "Pagamento" or "Financeiro"            => "Financeiro",
            "Comercial"                            => "Comercial",
            "Sugestão"                             => "Produto",
            _                                       => "Suporte",
        };

        var resumo = descricao.Length > 90 ? descricao[..90].Trim() + "…" : descricao.Trim();

        var totalHits = hitsCat + hitsPri;
        var confianca = totalHits switch
        {
            0 => 0.30,
            1 => 0.55,
            2 => 0.72,
            3 => 0.82,
            _ => Math.Min(0.95, 0.82 + (totalHits - 3) * 0.04),
        };

        return new ClassificacaoResultado(
            categoria, prioridade, departamento, resumo, confianca,
            $"Classificado como {categoria}/{prioridade} por heurística ({totalHits} termo(s) encontrado(s)).");
    }

    private static (string categoria, int hits) ClassificarCategoria(string texto)
    {
        var regras = new (string cat, string[] termos)[]
        {
            ("Login",       new[] { "login", "senha", "acesso", "logar", "autenticação", "entrar" }),
            ("Pagamento",   new[] { "pagamento", "cartão", "boleto", "pix", "cobrança", "transação" }),
            ("Financeiro",  new[] { "fatura", "reembolso", "assinatura", "nota fiscal", "financeiro" }),
            ("Performance", new[] { "lento", "travando", "demora", "performance", "lentidão", "timeout" }),
            ("Integração",  new[] { "integração", "api", "webhook", "sincroniz" }),
            ("Bug",         new[] { "erro", "bug", "falha", "quebrou", "não funciona", "crash" }),
            ("Cadastro",    new[] { "cadastro", "registrar", "atualizar dados", "endereço", "titularidade" }),
            ("Comercial",   new[] { "comercial", "parceria", "vendas", "orçamento", "contratar" }),
            ("Sugestão",    new[] { "sugestão", "gostaria", "poderia adicionar", "recurso", "feature" }),
        };

        var melhor = ("Outro", 0);
        foreach (var (cat, termos) in regras)
        {
            var hits = termos.Count(texto.Contains);
            if (hits > melhor.Item2) melhor = (cat, hits);
        }
        return melhor;
    }

    private static (string prioridade, int hits) ClassificarPrioridade(string texto)
    {
        var regras = new (string pri, string[] termos)[]
        {
            ("Crítica", new[] { "urgente", "imediato", "parado", "produção", "crítico", "não consigo trabalhar" }),
            ("Alta",    new[] { "importante", "rápido", "o quanto antes", "bloqueado" }),
            ("Baixa",   new[] { "quando puder", "sem pressa", "sugestão" }),
        };

        var melhor = ("Média", 0);
        foreach (var (pri, termos) in regras)
        {
            var hits = termos.Count(texto.Contains);
            if (hits > melhor.Item2) melhor = (pri, hits);
        }
        return melhor;
    }
}
