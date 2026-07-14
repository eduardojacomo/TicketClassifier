#!/usr/bin/env python3
"""Gera um CSV de tickets de suporte variados para teste do classificador."""
import csv, random

random.seed(42)

# (assunto, [descrições]) por tipo — com palavras-chave naturais.
TEMPLATES = {
    "Bug": [
        ("App trava ao abrir", "O aplicativo trava e fecha sozinho toda vez que tento abrir."),
        ("Erro ao salvar", "Aparece uma falha quando clico em salvar e o botão não funciona."),
        ("Tela em branco", "Depois do último update a tela fica em branco, parece um bug."),
        ("Não carrega os dados", "A listagem não carrega, dá erro 500 e some tudo."),
        ("Botão quebrado", "O botão de exportar não funciona, nada acontece ao clicar."),
        ("Crash no relatório", "Ao gerar o relatório o sistema quebra e trava a sessão."),
        ("Login com falha", "Não consigo logar, dá erro e a página recarrega sozinha."),
        ("Upload falhando", "O envio de arquivo falha sempre com mensagem de erro."),
    ],
    "Dúvida": [
        ("Como exportar relatório", "Como faço para exportar o relatório mensal em PDF? Onde fica essa opção?"),
        ("Onde altero a senha", "Gostaria de saber onde altero minha senha de acesso."),
        ("Dúvida sobre plano", "Qual a diferença entre os planos? Posso trocar quando quiser?"),
        ("Como convidar usuário", "Como posso convidar outro usuário para a minha conta?"),
        ("Configurar notificações", "Onde configuro para receber notificações por e-mail?"),
        ("Dúvida sobre integração", "Como funciona a integração com o meu sistema atual?"),
        ("Recuperar acesso", "Esqueci meu login, como faço para recuperar o acesso?"),
        ("Como filtrar dados", "Tem como filtrar os registros por data? Onde encontro isso?"),
    ],
    "Financeiro": [
        ("Cobrança indevida", "Fui cobrado duas vezes na fatura deste mês no cartão. Solicito o reembolso."),
        ("Boleto não chegou", "O boleto da assinatura não chegou, preciso pagar o pagamento."),
        ("Reembolso pendente", "Pedi o reembolso semana passada e ainda não caiu no cartão."),
        ("Erro na fatura", "O valor da fatura está diferente do combinado no plano."),
        ("Alterar forma de pagamento", "Quero trocar o cartão de crédito cadastrado para o pagamento."),
        ("Cancelar assinatura e reembolso", "Quero cancelar e receber o reembolso proporcional da assinatura."),
        ("Cobrança após cancelamento", "Cancelei mês passado mas continuo sendo cobrado na fatura."),
        ("Nota fiscal", "Preciso da nota fiscal do pagamento para o financeiro da empresa."),
    ],
    "Solicitação": [
        ("Sugestão de recurso", "Gostaria que o sistema permitisse adicionar tags nos tickets."),
        ("Pedido de nova feature", "Poderia adicionar um modo escuro? Seria um recurso muito útil."),
        ("Solicito aumento de limite", "Solicito o aumento do limite de usuários da minha conta."),
        ("Exportação em Excel", "Gostaria que tivesse a opção de exportar em Excel além de PDF."),
        ("Integração com Slack", "Poderiam adicionar integração com o Slack para avisos?"),
        ("Personalizar dashboard", "Solicito a possibilidade de personalizar os cards do dashboard."),
        ("API pública", "Gostaria de acesso a uma API para integrar com nosso sistema."),
        ("Novo relatório", "Poderia adicionar um relatório de produtividade por equipe?"),
    ],
    "Reclamação": [
        ("Péssimo atendimento", "Estou muito insatisfeito com o suporte, ninguém resolve nada."),
        ("Sistema sempre lento", "O sistema está horrível de lento, muito decepcionado."),
        ("Quero cancelar", "Cansei dos problemas, quero cancelar minha assinatura imediatamente."),
        ("Descaso com o cliente", "Faz dias que aguardo retorno e ninguém responde, um descaso."),
        ("Produto piorou", "Depois da atualização ficou péssimo, estou insatisfeito."),
        ("Reclamação formal", "Registro uma reclamação: o serviço não entrega o que promete."),
        ("Perdi dados importantes", "Perdi dados por causa de um erro de vocês, inadmissível."),
        ("Insatisfeito com cobrança", "Estou decepcionado, cobrança errada e ninguém resolve."),
    ],
    "Outros": [
        ("Atualização de cadastro", "Preciso atualizar o endereço e o telefone da minha conta."),
        ("Feedback geral", "Só queria deixar um elogio, o produto tem me ajudado bastante."),
        ("Parceria comercial", "Temos interesse em uma parceria comercial, com quem falo?"),
        ("Informação sobre evento", "Vocês vão participar de algum evento este ano?"),
        ("Contato do comercial", "Poderiam me passar o contato da equipe comercial?"),
        ("Mudança de titularidade", "Preciso transferir a titularidade da conta para outra pessoa."),
    ],
}

# Modificadores de prioridade que aparecem no início/fim da descrição.
URGENTE = [" Está tudo parado em produção, urgente!", " Não consigo trabalhar, preciso de solução imediata.", " Crítico, resolvam o quanto antes!"]
ALTA    = [" É importante, preciso resolver rápido.", " Estou bloqueado por causa disso.", " Aguardo com prioridade, por favor."]
BAIXA   = [" Sem pressa, quando puderem.", " É só uma dúvida, pode responder com calma.", " Apenas uma sugestão, sem urgência."]

def _typos(texto):
    """Introduz pequenos ruídos: troca de letras, remoção de acentos."""
    if random.random() < 0.5:
        texto = texto.replace("ã", "a").replace("ç", "c").replace("é", "e")
    if random.random() < 0.3 and len(texto) > 10:
        i = random.randint(0, len(texto) - 2)
        texto = texto[:i] + texto[i + 1] + texto[i] + texto[i + 2:]
    return texto

def _ruido(assunto, desc):
    r = random.random()
    if r < 0.10:                      # assunto vazio
        assunto = ""
    if r < 0.08:                      # caixa alta (cliente irritado)
        desc = desc.upper()
    if 0.10 <= r < 0.20:              # dois temas no mesmo ticket
        outro_tipo = random.choice(list(TEMPLATES))
        _, d2 = random.choice(TEMPLATES[outro_tipo])
        desc = desc + " Além disso, " + d2[0].lower() + d2[1:]
    if random.random() < 0.25:        # typos
        desc = _typos(desc)
    return assunto, desc

def gerar(n=1500, com_ruido=True):
    linhas = []
    tipos = list(TEMPLATES.keys())
    for i in range(1, n + 1):
        tipo = random.choices(tipos, weights=[22, 20, 20, 15, 15, 8])[0]
        assunto, desc = random.choice(TEMPLATES[tipo])
        r = random.random()
        if r < 0.15:
            desc += random.choice(URGENTE)
        elif r < 0.40:
            desc += random.choice(ALTA)
        elif r < 0.60:
            desc += random.choice(BAIXA)
        if com_ruido:
            assunto, desc = _ruido(assunto, desc)
        linhas.append((i, assunto, desc))
    return linhas

def escrever(nome, n, com_ruido):
    with open(nome, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f, quoting=csv.QUOTE_MINIMAL)
        w.writerow(["id", "subject", "description"])
        for row in gerar(n, com_ruido):
            w.writerow(row)
    print(f"Gerado {nome} com {n} tickets (ruido={com_ruido}).")

if __name__ == "__main__":
    import sys
    n = int(sys.argv[1]) if len(sys.argv) > 1 else 1500
    escrever("sample-tickets-large.csv", 400, com_ruido=False)
    escrever("sample-tickets-xl.csv", n, com_ruido=True)
