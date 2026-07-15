using System.Text;
using TicketClassifier.Api.Services;

namespace TicketClassifier.Tests;

public class CsvServiceTests
{
    private readonly CsvService _sut = new();

    private static MemoryStream ToStream(string content, Encoding? encoding = null)
    {
        var bytes = (encoding ?? Encoding.UTF8).GetBytes(content);
        return new MemoryStream(bytes);
    }

    // ── Valid files ─────────────────────────────────────────────────

    [Fact]
    public void Parse_CsvValido_RetornaTickets()
    {
        var csv = "subject,description\nLogin error,Cannot access my account\nPayment,Duplicate billing";
        var result = _sut.Parse(ToStream(csv));

        Assert.Equal(2, result.Count);
        Assert.Equal("Login error", result[0].Assunto);
        Assert.Equal("Cannot access my account", result[0].Descricao);
    }

    [Fact]
    public void Parse_CsvComPontoEVirgula_RetornaTickets()
    {
        var csv = "subject;description\nError;System issue\nBug;Broken screen";
        var result = _sut.Parse(ToStream(csv));

        Assert.Equal(2, result.Count);
        Assert.Equal("Error", result[0].Assunto);
    }

    [Fact]
    public void Parse_CsvComTab_RetornaTickets()
    {
        var csv = "subject\tdescription\nError\tSystem issue";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
    }

    [Fact]
    public void Parse_CsvComId_PreservaExternalId()
    {
        var csv = "id,subject,description\nTK-001,Error,Issue";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Equal("TK-001", result[0].ExternalId);
    }

    [Fact]
    public void Parse_HeadersAlternativos_Reconhece()
    {
        var csv = "titulo,body\nLogin error,Cannot access";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Equal("Login error", result[0].Assunto);
        Assert.Equal("Cannot access", result[0].Descricao);
    }

    [Fact]
    public void Parse_HeaderDescription_Reconhece()
    {
        var csv = "description\nSystem issue";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Equal("System issue", result[0].Descricao);
    }

    [Fact]
    public void Parse_SemColunaReconhecida_UsaFallbackConcatenado()
    {
        var csv = "campo1,campo2\nvalue1,value2";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Contains("value1", result[0].Descricao);
        Assert.Contains("value2", result[0].Descricao);
    }

    // ── Special characters and encoding ──────────────────────────────

    [Fact]
    public void Parse_CaracteresEspeciais_MantemConteudo()
    {
        var csv = "subject,description\n\"Error: login & password\",\"User cannot <access> the 'system'\"";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Equal("Error: login & password", result[0].Assunto);
        Assert.Contains("<access>", result[0].Descricao);
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
        var csv = "subject,description\nIssue 🐛,Critical error 💥 in the system";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Contains("🐛", result[0].Assunto);
        Assert.Contains("💥", result[0].Descricao);
    }

    [Fact]
    public void Parse_Utf8ComBom_FuncionaCorreto()
    {
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var conteudo = Encoding.UTF8.GetBytes("subject,description\nError,Issue");
        var bytes = bom.Concat(conteudo).ToArray();

        var result = _sut.Parse(new MemoryStream(bytes));

        Assert.Single(result);
        Assert.Equal("Error", result[0].Assunto);
    }

    [Fact]
    public void Parse_Latin1_FuncionaCorreto()
    {
        var latin1 = Encoding.Latin1;
        var csv = "subject,description\nInstallation,Not working";
        var bytes = latin1.GetBytes(csv);

        var result = _sut.Parse(new MemoryStream(bytes));

        Assert.Single(result);
    }

    [Fact]
    public void Parse_Utf16Le_FuncionaCorreto()
    {
        var csv = "subject,description\nError,System issue";
        var bom = new byte[] { 0xFF, 0xFE };
        var conteudo = Encoding.Unicode.GetBytes(csv);
        var bytes = bom.Concat(conteudo).ToArray();

        var result = _sut.Parse(new MemoryStream(bytes));

        Assert.Single(result);
    }

    // ── Quoted fields and multiline ───────────────────────────────

    [Fact]
    public void Parse_CampoComAspas_ParseiaCorreto()
    {
        var csv = "subject,description\n\"Critical, error\",\"Line 1\nLine 2\"";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Equal("Critical, error", result[0].Assunto);
        Assert.Contains("Line 1", result[0].Descricao);
        Assert.Contains("Line 2", result[0].Descricao);
    }

    [Fact]
    public void Parse_CampoComAspasInternas_ParseiaCorreto()
    {
        var csv = "subject,description\n\"Error with \"\"quotes\"\"\",Issue";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Contains("quotes", result[0].Assunto);
    }

    // ── Invalid and empty lines ────────────────────────────────────

    [Fact]
    public void Parse_LinhasVazias_SaoIgnoradas()
    {
        var csv = "subject,description\nError,Issue\n\n  \n\nBug,Another issue";
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
        var csv = "subject,description\nError,Issue\n,\nBug,Failure\n , ";
        var result = _sut.Parse(ToStream(csv));

        Assert.Equal(2, result.Count);
    }

    // ── Format validations ────────────────────────────────────────

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
        bytes[0] = 0x50; // some non-null byte

        var ex = Assert.Throws<CsvValidationException>(() => _sut.Parse(new MemoryStream(bytes)));
        Assert.Equal("ARQUIVO_BINARIO", ex.Codigo);
    }

    [Fact]
    public void Parse_ArquivoJson_LancaExcecao()
    {
        var json = "[{\"subject\":\"test\"}]";
        var ex = Assert.Throws<CsvValidationException>(() => _sut.Parse(ToStream(json)));
        Assert.Equal("FORMATO_JSON", ex.Codigo);
    }

    [Fact]
    public void Parse_ArquivoJsonObjeto_LancaExcecao()
    {
        var json = "{\"data\":[{\"subject\":\"test\"}]}";
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

    // ── Limit validations ─────────────────────────────────────────

    [Fact]
    public void Parse_CampoMuitoGrande_Trunca()
    {
        var description = new string('A', CsvService.MaxTamanhoCampo + 1000);
        var csv = $"description\n{description}";

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

    // ── Internal methods ─────────────────────────────────────────

    [Fact]
    public void ValidarConteudoBinario_TextoNormal_NaoLanca()
    {
        var bytes = Encoding.UTF8.GetBytes("subject,description\nError,Issue");
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
        var bytes = Encoding.UTF8.GetBytes("test accentuation");
        var result = CsvService.DecodificarTexto(bytes);
        Assert.Contains("accentuation", result);
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
        CsvService.ValidarConteudoTexto("subject,description\nError,Issue");
    }

    [Fact]
    public void ValidarConteudoTexto_Vazio_Lanca()
    {
        Assert.Throws<CsvValidationException>(() => CsvService.ValidarConteudoTexto("   "));
    }

    [Fact]
    public void LimparTexto_RemoveCaracteresInvisiveis()
    {
        var text = "﻿subject​";
        var result = CsvService.LimparTexto(text);
        Assert.Equal("subject", result);
    }

    [Fact]
    public void LimparCampo_RemoveNulos()
    {
        var field = "text\0with\0nulls";
        var result = CsvService.LimparCampo(field);
        Assert.Equal("textwithnulls", result);
    }

    [Fact]
    public void LimparCampo_Null_RetornaNull()
    {
        Assert.Null(CsvService.LimparCampo(null));
    }

    // ── Edge cases ───────────────────────────────────────────────

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
        var csv = " subject , description \nError,Issue";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
    }

    [Fact]
    public void Parse_WindowsLineEndings_FuncionaCorreto()
    {
        var csv = "subject,description\r\nError,Issue\r\nBug,Failure";
        var result = _sut.Parse(ToStream(csv));

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Parse_GrandeQuantidadeDeLinhas_Funciona()
    {
        var sb = new StringBuilder("subject,description\n");
        for (var i = 0; i < 1000; i++)
            sb.AppendLine($"Ticket {i},Description of ticket {i}");

        var result = _sut.Parse(ToStream(sb.ToString()));
        Assert.Equal(1000, result.Count);
    }

    [Fact]
    public void Parse_CamposComQuebraDeLinha_MantemConteudo()
    {
        var csv = "subject,description\n\"Error\",\"Line 1\nLine 2\nLine 3\"";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Contains("Line 1", result[0].Descricao);
        Assert.Contains("Line 3", result[0].Descricao);
    }

    [Fact]
    public void Parse_CaracteresDeControle_SaoTratados()
    {
        var csv = "subject,description\nError\x01\x02,Issue\x03";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
    }
}
