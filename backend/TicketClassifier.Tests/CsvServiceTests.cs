using System.Text;
using TicketClassifier.Api.Services;

namespace TicketClassifier.Tests;

public class CsvServiceTests
{
    private readonly CsvService _sut = new();

    private static MemoryStream ToStream(string conteudo, Encoding? encoding = null)
    {
        var bytes = (encoding ?? Encoding.UTF8).GetBytes(conteudo);
        return new MemoryStream(bytes);
    }

    // ── Arquivos válidos ─────────────────────────────────────────────────

    [Fact]
    public void Parse_CsvValido_RetornaTickets()
    {
        var csv = "subject,description\nErro no login,Não consigo acessar minha conta\nPagamento,Cobrança duplicada";
        var result = _sut.Parse(ToStream(csv));

        Assert.Equal(2, result.Count);
        Assert.Equal("Erro no login", result[0].Assunto);
        Assert.Equal("Não consigo acessar minha conta", result[0].Descricao);
    }

    [Fact]
    public void Parse_CsvComPontoEVirgula_RetornaTickets()
    {
        var csv = "subject;description\nErro;Problema no sistema\nBug;Tela quebrada";
        var result = _sut.Parse(ToStream(csv));

        Assert.Equal(2, result.Count);
        Assert.Equal("Erro", result[0].Assunto);
    }

    [Fact]
    public void Parse_CsvComTab_RetornaTickets()
    {
        var csv = "subject\tdescription\nErro\tProblema no sistema";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
    }

    [Fact]
    public void Parse_CsvComId_PreservaExternalId()
    {
        var csv = "id,subject,description\nTK-001,Erro,Problema";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Equal("TK-001", result[0].ExternalId);
    }

    [Fact]
    public void Parse_HeadersAlternativos_Reconhece()
    {
        var csv = "titulo,body\nErro no login,Não consigo acessar";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Equal("Erro no login", result[0].Assunto);
        Assert.Equal("Não consigo acessar", result[0].Descricao);
    }

    [Fact]
    public void Parse_HeaderDescription_Reconhece()
    {
        var csv = "description\nProblema no sistema";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Equal("Problema no sistema", result[0].Descricao);
    }

    [Fact]
    public void Parse_SemColunaReconhecida_UsaFallbackConcatenado()
    {
        var csv = "campo1,campo2\nvalor1,valor2";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Contains("valor1", result[0].Descricao);
        Assert.Contains("valor2", result[0].Descricao);
    }

    // ── Caracteres especiais e encoding ──────────────────────────────────

    [Fact]
    public void Parse_CaracteresEspeciais_MantemConteudo()
    {
        var csv = "subject,description\n\"Erro: login & senha\",\"Usuário não consegue <acessar> o 'sistema'\"";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Equal("Erro: login & senha", result[0].Assunto);
        Assert.Contains("<acessar>", result[0].Descricao);
    }

    [Fact]
    public void Parse_Acentuacao_MantemConteudo()
    {
        var csv = "subject,description\nção,Não está funcionando à noite — ñ";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Contains("Não está funcionando", result[0].Descricao);
    }

    [Fact]
    public void Parse_Emoji_MantemConteudo()
    {
        var csv = "subject,description\nProblema 🐛,Erro grave 💥 no sistema";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Contains("🐛", result[0].Assunto);
        Assert.Contains("💥", result[0].Descricao);
    }

    [Fact]
    public void Parse_Utf8ComBom_FuncionaCorreto()
    {
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var conteudo = Encoding.UTF8.GetBytes("subject,description\nErro,Problema");
        var bytes = bom.Concat(conteudo).ToArray();

        var result = _sut.Parse(new MemoryStream(bytes));

        Assert.Single(result);
        Assert.Equal("Erro", result[0].Assunto);
    }

    [Fact]
    public void Parse_Latin1_FuncionaCorreto()
    {
        var latin1 = Encoding.Latin1;
        var csv = "subject,description\nInstalaçao,Nao funciona";
        var bytes = latin1.GetBytes(csv);

        var result = _sut.Parse(new MemoryStream(bytes));

        Assert.Single(result);
    }

    [Fact]
    public void Parse_Utf16Le_FuncionaCorreto()
    {
        var csv = "subject,description\nErro,Problema no sistema";
        var bom = new byte[] { 0xFF, 0xFE };
        var conteudo = Encoding.Unicode.GetBytes(csv);
        var bytes = bom.Concat(conteudo).ToArray();

        var result = _sut.Parse(new MemoryStream(bytes));

        Assert.Single(result);
    }

    // ── Campos com aspas e multilinhas ───────────────────────────────────

    [Fact]
    public void Parse_CampoComAspas_ParseiaCorreto()
    {
        var csv = "subject,description\n\"Erro, grave\",\"Linha 1\nLinha 2\"";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Equal("Erro, grave", result[0].Assunto);
        Assert.Contains("Linha 1", result[0].Descricao);
        Assert.Contains("Linha 2", result[0].Descricao);
    }

    [Fact]
    public void Parse_CampoComAspasInternas_ParseiaCorreto()
    {
        var csv = "subject,description\n\"Erro com \"\"aspas\"\"\",Problema";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Contains("aspas", result[0].Assunto);
    }

    // ── Linhas inválidas e vazias ────────────────────────────────────────

    [Fact]
    public void Parse_LinhasVazias_SaoIgnoradas()
    {
        var csv = "subject,description\nErro,Problema\n\n  \n\nBug,Outro problema";
        var result = _sut.Parse(ToStream(csv));

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Parse_LinhasSemConteudoUtil_SaoIgnoradas()
    {
        var csv = "subject,description\n,\n , ";
        var result = _sut.Parse(ToStream(csv));

        Assert.Empty(result);
    }

    [Fact]
    public void Parse_MisturaLinhasBoasERuins_RetornaApenasBoas()
    {
        var csv = "subject,description\nErro,Problema\n,\nBug,Falha\n , ";
        var result = _sut.Parse(ToStream(csv));

        Assert.Equal(2, result.Count);
    }

    // ── Validações de formato ────────────────────────────────────────────

    [Fact]
    public void Parse_ArquivoVazio_LancaExcecao()
    {
        var ex = Assert.Throws<CsvValidationException>(() => _sut.Parse(new MemoryStream()));
        Assert.Equal("ARQUIVO_VAZIO", ex.Codigo);
    }

    [Fact]
    public void Parse_StreamNull_LancaExcecao()
    {
        Assert.Throws<CsvValidationException>(() => _sut.Parse(null!));
    }

    [Fact]
    public void Parse_ArquivoBinario_LancaExcecao()
    {
        var bytes = new byte[500];
        Array.Fill(bytes, (byte)0x00);
        bytes[0] = 0x50; // algum byte não-nulo

        var ex = Assert.Throws<CsvValidationException>(() => _sut.Parse(new MemoryStream(bytes)));
        Assert.Equal("ARQUIVO_BINARIO", ex.Codigo);
    }

    [Fact]
    public void Parse_ArquivoJson_LancaExcecao()
    {
        var json = "[{\"subject\":\"teste\"}]";
        var ex = Assert.Throws<CsvValidationException>(() => _sut.Parse(ToStream(json)));
        Assert.Equal("FORMATO_JSON", ex.Codigo);
    }

    [Fact]
    public void Parse_ArquivoJsonObjeto_LancaExcecao()
    {
        var json = "{\"data\":[{\"subject\":\"teste\"}]}";
        var ex = Assert.Throws<CsvValidationException>(() => _sut.Parse(ToStream(json)));
        Assert.Equal("FORMATO_JSON", ex.Codigo);
    }

    [Fact]
    public void Parse_ArquivoXml_LancaExcecao()
    {
        var xml = "<?xml version=\"1.0\"?><data></data>";
        var ex = Assert.Throws<CsvValidationException>(() => _sut.Parse(ToStream(xml)));
        Assert.Equal("FORMATO_XML_HTML", ex.Codigo);
    }

    [Fact]
    public void Parse_ArquivoHtml_LancaExcecao()
    {
        var html = "<html><body>test</body></html>";
        var ex = Assert.Throws<CsvValidationException>(() => _sut.Parse(ToStream(html)));
        Assert.Equal("FORMATO_XML_HTML", ex.Codigo);
    }

    [Fact]
    public void Parse_ArquivoPdf_LancaExcecao()
    {
        var pdf = "%PDF-1.4 fake content";
        var ex = Assert.Throws<CsvValidationException>(() => _sut.Parse(ToStream(pdf)));
        Assert.Equal("FORMATO_PDF", ex.Codigo);
    }

    // ── Validações de limite ─────────────────────────────────────────────

    [Fact]
    public void Parse_CampoMuitoGrande_Trunca()
    {
        var descricao = new string('A', CsvService.MaxTamanhoCampo + 1000);
        var csv = $"description\n{descricao}";

        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Equal(CsvService.MaxTamanhoCampo, result[0].Descricao.Length);
    }

    [Fact]
    public void Parse_ExcessoColunas_LancaExcecao()
    {
        var headers = string.Join(",", Enumerable.Range(0, CsvService.MaxColunas + 1).Select(i => $"col{i}"));
        var values = string.Join(",", Enumerable.Range(0, CsvService.MaxColunas + 1).Select(i => $"val{i}"));
        var csv = $"{headers}\n{values}";

        var ex = Assert.Throws<CsvValidationException>(() => _sut.Parse(ToStream(csv)));
        Assert.Equal("EXCESSO_COLUNAS", ex.Codigo);
    }

    // ── Métodos internos ─────────────────────────────────────────────────

    [Fact]
    public void ValidarConteudoBinario_TextoNormal_NaoLanca()
    {
        var bytes = Encoding.UTF8.GetBytes("subject,description\nErro,Problema");
        CsvService.ValidarConteudoBinario(bytes);
    }

    [Fact]
    public void ValidarConteudoBinario_MuitosNulos_Lanca()
    {
        var bytes = new byte[200];
        Array.Fill(bytes, (byte)0x00);

        Assert.Throws<CsvValidationException>(() => CsvService.ValidarConteudoBinario(bytes));
    }

    [Fact]
    public void DecodificarTexto_Utf8SemBom_Funciona()
    {
        var bytes = Encoding.UTF8.GetBytes("teste acentuação");
        var result = CsvService.DecodificarTexto(bytes);
        Assert.Contains("acentuação", result);
    }

    [Fact]
    public void DecodificarTexto_Latin1_FallbackFunciona()
    {
        var bytes = Encoding.Latin1.GetBytes("café résumé");
        var result = CsvService.DecodificarTexto(bytes);
        Assert.Contains("caf", result);
    }

    [Fact]
    public void ValidarConteudoTexto_TextoNormal_NaoLanca()
    {
        CsvService.ValidarConteudoTexto("subject,description\nErro,Problema");
    }

    [Fact]
    public void ValidarConteudoTexto_Vazio_Lanca()
    {
        Assert.Throws<CsvValidationException>(() => CsvService.ValidarConteudoTexto("   "));
    }

    [Fact]
    public void LimparTexto_RemoveCaracteresInvisiveis()
    {
        var texto = "﻿subject​";
        var result = CsvService.LimparTexto(texto);
        Assert.Equal("subject", result);
    }

    [Fact]
    public void LimparCampo_RemoveNulos()
    {
        var campo = "texto\0com\0nulos";
        var result = CsvService.LimparCampo(campo);
        Assert.Equal("textocomnulos", result);
    }

    [Fact]
    public void LimparCampo_Null_RetornaNull()
    {
        Assert.Null(CsvService.LimparCampo(null));
    }

    // ── Edge cases ───────────────────────────────────────────────────────

    [Fact]
    public void Parse_ApenasCabecalhoSemDados_RetornaListaVazia()
    {
        var csv = "subject,description";
        var result = _sut.Parse(ToStream(csv));
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_CabecalhoComEspacos_FuncionaCorreto()
    {
        var csv = " subject , description \nErro,Problema";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
    }

    [Fact]
    public void Parse_WindowsLineEndings_FuncionaCorreto()
    {
        var csv = "subject,description\r\nErro,Problema\r\nBug,Falha";
        var result = _sut.Parse(ToStream(csv));

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Parse_GrandeQuantidadeDeLinhas_Funciona()
    {
        var sb = new StringBuilder("subject,description\n");
        for (var i = 0; i < 1000; i++)
            sb.AppendLine($"Ticket {i},Descricao do ticket {i}");

        var result = _sut.Parse(ToStream(sb.ToString()));
        Assert.Equal(1000, result.Count);
    }

    [Fact]
    public void Parse_CamposComQuebraDeLinha_MantemConteudo()
    {
        var csv = "subject,description\n\"Erro\",\"Linha 1\nLinha 2\nLinha 3\"";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Contains("Linha 1", result[0].Descricao);
        Assert.Contains("Linha 3", result[0].Descricao);
    }

    [Fact]
    public void Parse_CaracteresDeControle_SaoTratados()
    {
        var csv = "subject,description\nErro\x01\x02,Problema\x03";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
    }
}
