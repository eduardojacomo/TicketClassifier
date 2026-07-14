using System.Text;
using TicketClassifier.Api.Gateways.Interface;

namespace TicketClassifier.Api.Prompts;

public static class ClassificacaoPromptBuilder
{
    public static string ConstruirLote(IReadOnlyList<TicketParaClassificar> itens, int loteAtual = 1, int totalLotes = 1, int totalTickets = 0)
    {
        var lista = new StringBuilder();
        foreach (var t in itens)
        {
            var desc = t.Descricao.Length > 600 ? t.Descricao[..600] : t.Descricao;
            lista.Append($"[{t.Indice}] Assunto: {t.Assunto} | Descrição: {desc}\n");
        }

        var totalReal = totalTickets > 0 ? totalTickets : itens.Count;

        return
            "Você é um analista de suporte técnico experiente. Classifique CADA ticket da lista com precisão.\n\n" +
            $"CONTEXTO DO LOTE: Você está processando o lote {loteAtual} de {totalLotes} " +
            $"(total de {totalReal} tickets no arquivo). Este lote contém {itens.Count} tickets. " +
            "Responda TODOS os tickets deste lote — não omita nenhum.\n\n" +
            "Responda SOMENTE com um ARRAY JSON válido (sem markdown, sem ```). Um objeto por ticket, " +
            "incluindo o campo \"indice\" IGUAL ao número entre colchetes do ticket. Use EXATAMENTE um dos " +
            "valores permitidos, com a mesma acentuação.\n\n" +
            $"categoria: {string.Join(" | ", Categorias.Lista)}\n" +
            $"prioridade: {string.Join(" | ", Categorias.Prioridades)}\n" +
            $"departamento: {string.Join(" | ", Categorias.Departamentos)}\n" +
            $"sentimento: {string.Join(" | ", Categorias.Sentimentos)}\n\n" +
            "REGRAS DE CATEGORIA (aplique com atenção — o padrão NÃO é 'Outro'):\n" +
            "- Dúvida: o cliente faz uma PERGUNTA ou pede orientação. Indicadores: \"como faço\", \"onde fica\", \"é possível\", \"?\", \"não sei como\", \"me explica\". " +
            "IMPORTANTE: se o texto é uma pergunta sobre como usar algo e NÃO relata um erro/falha, a categoria é Dúvida, não Bug.\n" +
            "- Bug: o cliente relata um DEFEITO, erro, travamento, crash. O sistema não funciona como deveria. Indicadores: \"erro\", \"trava\", \"crash\", \"não funciona\", \"fecha sozinho\".\n" +
            "- Reclamação: o cliente expressa INSATISFAÇÃO com o serviço/atendimento. Tom de frustração ou raiva. Indicadores: \"péssimo\", \"insatisfeito\", \"ninguém resolve\", \"absurdo\".\n" +
            "- Elogio: o cliente expressa SATISFAÇÃO, agradecimento ou elogia o serviço.\n" +
            "- Login: problemas especificamente com autenticação, senha, acesso à conta.\n" +
            "- Pagamento: problemas com transações, cobranças, cartão, boleto, PIX.\n" +
            "- Financeiro: faturas, reembolsos, assinaturas, notas fiscais.\n" +
            "- Performance: lentidão, demora, timeout.\n" +
            "- Integração: APIs, webhooks, sincronização entre sistemas.\n" +
            "- Cadastro: registro de conta, atualização de dados pessoais.\n" +
            "- Comercial: vendas, parcerias, orçamentos, propostas.\n" +
            "- Sugestão: pedido de funcionalidade ou melhoria.\n" +
            "- Outro: SOMENTE se nenhuma das categorias acima se aplica.\n\n" +
            "Critérios de PRIORIDADE (não use 'Média' por padrão — escolha de fato):\n" +
            "- Crítica: sistema/serviço parado, produção afetada, perda de dados, urgência explícita.\n" +
            "- Alta: bloqueia o trabalho do cliente, erro grave, cobrança indevida, precisa de solução rápida.\n" +
            "- Média: problema comum sem bloqueio total, dúvida relevante.\n" +
            "- Baixa: dúvida simples, sugestão, sem urgência.\n\n" +
            "Campo \"sentimento\": analise o tom do texto do cliente.\n" +
            "- positivo: elogio, satisfação, agradecimento, sugestão construtiva.\n" +
            "- negativo: reclamação, frustração, raiva, insatisfação, urgência com irritação.\n" +
            "- neutro: tom informativo, dúvida objetiva, solicitação factual.\n\n" +
            "Campo \"tags\": array com 2 a 5 palavras-chave relevantes extraídas do ticket (em minúsculas). " +
            "Devem representar os temas centrais do ticket (ex: [\"login\",\"senha\",\"erro\"], [\"cobrança\",\"indevida\"], [\"relatório\",\"exportar\",\"pdf\"]).\n\n" +
            "Campo \"confianca\" (0.0 a 1.0): indica o quão seguro você está na classificação.\n" +
            "- 0.90–1.00: ticket muito claro, palavras-chave explícitas, classificação óbvia.\n" +
            "- 0.75–0.89: boa certeza, contexto suficiente para decidir.\n" +
            "- 0.50–0.74: ambíguo, poderia ser outra categoria/prioridade.\n" +
            "- abaixo de 0.50: muito vago, informação insuficiente.\n" +
            "Varie a confiança de acordo com a clareza de CADA ticket. NÃO use o mesmo valor para todos.\n\n" +
            "Formato de cada item:\n" +
            "{\"indice\":0,\"categoria\":\"\",\"prioridade\":\"\",\"departamento\":\"\",\"resumo\":\"\",\"sentimento\":\"\",\"tags\":[],\"confianca\":0.85,\"justificativa\":\"\"}\n\n" +
            "--- TICKETS ---\n" + lista.ToString() + "--- FIM ---";
    }
}
