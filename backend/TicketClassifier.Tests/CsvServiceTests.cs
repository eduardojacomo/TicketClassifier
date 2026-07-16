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
    public void Parse_ValidCsv_ReturnsTickets()
    {
        var csv = "subject,description\nLogin error,Cannot access my account\nPayment,Duplicate billing";
        var result = _sut.Parse(ToStream(csv));

        Assert.Equal(2, result.Count);
        Assert.Equal("Login error", result[0].Subject);
        Assert.Equal("Cannot access my account", result[0].Description);
    }

    [Fact]
    public void Parse_CsvWithSemicolon_ReturnsTickets()
    {
        var csv = "subject;description\nError;System issue\nBug;Broken screen";
        var result = _sut.Parse(ToStream(csv));

        Assert.Equal(2, result.Count);
        Assert.Equal("Error", result[0].Subject);
    }

    [Fact]
    public void Parse_CsvWithTab_ReturnsTickets()
    {
        var csv = "subject\tdescription\nError\tSystem issue";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
    }

    [Fact]
    public void Parse_CsvWithId_PreservesExternalId()
    {
        var csv = "id,subject,description\nTK-001,Error,Issue";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Equal("TK-001", result[0].ExternalId);
    }

    [Fact]
    public void Parse_AlternativeHeaders_Recognizes()
    {
        var csv = "titulo,body\nLogin error,Cannot access";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Equal("Login error", result[0].Subject);
        Assert.Equal("Cannot access", result[0].Description);
    }

    [Fact]
    public void Parse_DescriptionHeader_Recognizes()
    {
        var csv = "description\nSystem issue";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Equal("System issue", result[0].Description);
    }

    [Fact]
    public void Parse_NoRecognizedColumn_UsesConcatenatedFallback()
    {
        var csv = "campo1,campo2\nvalue1,value2";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Contains("value1", result[0].Description);
        Assert.Contains("value2", result[0].Description);
    }

    // ── Special characters and encoding ──────────────────────────────

    [Fact]
    public void Parse_SpecialCharacters_KeepsContent()
    {
        var csv = "subject,description\n\"Error: login & password\",\"User cannot <access> the 'system'\"";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Equal("Error: login & password", result[0].Subject);
        Assert.Contains("<access>", result[0].Description);
    }

    [Fact]
    public void Parse_Accents_KeepsContent()
    {
        var csv = "subject,description\nção,Não está funcionando à noite — ñ";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Contains("Não está funcionando", result[0].Description);
    }

    [Fact]
    public void Parse_Emoji_KeepsContent()
    {
        var csv = "subject,description\nIssue 🐛,Critical error 💥 in the system";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Contains("🐛", result[0].Subject);
        Assert.Contains("💥", result[0].Description);
    }

    [Fact]
    public void Parse_Utf8WithBom_WorksCorrectly()
    {
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var content = Encoding.UTF8.GetBytes("subject,description\nError,Issue");
        var bytes = bom.Concat(content).ToArray();

        var result = _sut.Parse(new MemoryStream(bytes));

        Assert.Single(result);
        Assert.Equal("Error", result[0].Subject);
    }

    [Fact]
    public void Parse_Latin1_WorksCorrectly()
    {
        var latin1 = Encoding.Latin1;
        var csv = "subject,description\nInstallation,Not working";
        var bytes = latin1.GetBytes(csv);

        var result = _sut.Parse(new MemoryStream(bytes));

        Assert.Single(result);
    }

    [Fact]
    public void Parse_Utf16Le_WorksCorrectly()
    {
        var csv = "subject,description\nError,System issue";
        var bom = new byte[] { 0xFF, 0xFE };
        var content = Encoding.Unicode.GetBytes(csv);
        var bytes = bom.Concat(content).ToArray();

        var result = _sut.Parse(new MemoryStream(bytes));

        Assert.Single(result);
    }

    // ── Quoted fields and multiline ───────────────────────────────

    [Fact]
    public void Parse_FieldWithQuotes_ParsesCorrectly()
    {
        var csv = "subject,description\n\"Critical, error\",\"Line 1\nLine 2\"";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Equal("Critical, error", result[0].Subject);
        Assert.Contains("Line 1", result[0].Description);
        Assert.Contains("Line 2", result[0].Description);
    }

    [Fact]
    public void Parse_FieldWithInternalQuotes_ParsesCorrectly()
    {
        var csv = "subject,description\n\"Error with \"\"quotes\"\"\",Issue";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Contains("quotes", result[0].Subject);
    }

    // ── Invalid and empty lines ────────────────────────────────────

    [Fact]
    public void Parse_EmptyLines_AreIgnored()
    {
        var csv = "subject,description\nError,Issue\n\n  \n\nBug,Another issue";
        var result = _sut.Parse(ToStream(csv));

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Parse_LinesWithoutUsefulContent_AreIgnored()
    {
        var csv = "subject,description\n,\n , ";
        var result = _sut.Parse(ToStream(csv));

        Assert.Empty(result);
    }

    [Fact]
    public void Parse_MixOfGoodAndBadLines_ReturnsOnlyGood()
    {
        var csv = "subject,description\nError,Issue\n,\nBug,Failure\n , ";
        var result = _sut.Parse(ToStream(csv));

        Assert.Equal(2, result.Count);
    }

    // ── Format validations ────────────────────────────────────────

    [Fact]
    public void Parse_EmptyFile_ThrowsException()
    {
        var ex = Assert.Throws<CsvValidationException>(() => _sut.Parse(new MemoryStream()));
        Assert.Equal("ARQUIVO_VAZIO", ex.Code);
    }

    [Fact]
    public void Parse_NullStream_ThrowsException()
    {
        Assert.Throws<CsvValidationException>(() => _sut.Parse(null!));
    }

    [Fact]
    public void Parse_BinaryFile_ThrowsException()
    {
        var bytes = new byte[500];
        Array.Fill(bytes, (byte)0x00);
        bytes[0] = 0x50; // some non-null byte

        var ex = Assert.Throws<CsvValidationException>(() => _sut.Parse(new MemoryStream(bytes)));
        Assert.Equal("ARQUIVO_BINARIO", ex.Code);
    }

    [Fact]
    public void Parse_JsonFile_ThrowsException()
    {
        var json = "[{\"subject\":\"test\"}]";
        var ex = Assert.Throws<CsvValidationException>(() => _sut.Parse(ToStream(json)));
        Assert.Equal("FORMATO_JSON", ex.Code);
    }

    [Fact]
    public void Parse_JsonObjectFile_ThrowsException()
    {
        var json = "{\"data\":[{\"subject\":\"test\"}]}";
        var ex = Assert.Throws<CsvValidationException>(() => _sut.Parse(ToStream(json)));
        Assert.Equal("FORMATO_JSON", ex.Code);
    }

    [Fact]
    public void Parse_XmlFile_ThrowsException()
    {
        var xml = "<?xml version=\"1.0\"?><data></data>";
        var ex = Assert.Throws<CsvValidationException>(() => _sut.Parse(ToStream(xml)));
        Assert.Equal("FORMATO_XML_HTML", ex.Code);
    }

    [Fact]
    public void Parse_HtmlFile_ThrowsException()
    {
        var html = "<html><body>test</body></html>";
        var ex = Assert.Throws<CsvValidationException>(() => _sut.Parse(ToStream(html)));
        Assert.Equal("FORMATO_XML_HTML", ex.Code);
    }

    [Fact]
    public void Parse_PdfFile_ThrowsException()
    {
        var pdf = "%PDF-1.4 fake content";
        var ex = Assert.Throws<CsvValidationException>(() => _sut.Parse(ToStream(pdf)));
        Assert.Equal("FORMATO_PDF", ex.Code);
    }

    // ── Limit validations ─────────────────────────────────────────

    [Fact]
    public void Parse_FieldTooLarge_Truncates()
    {
        var description = new string('A', CsvService.MaxFieldSize + 1000);
        var csv = $"description\n{description}";

        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Equal(CsvService.MaxFieldSize, result[0].Description.Length);
    }

    [Fact]
    public void Parse_TooManyColumns_ThrowsException()
    {
        var headers = string.Join(",", Enumerable.Range(0, CsvService.MaxColumns + 1).Select(i => $"col{i}"));
        var values = string.Join(",", Enumerable.Range(0, CsvService.MaxColumns + 1).Select(i => $"val{i}"));
        var csv = $"{headers}\n{values}";

        var ex = Assert.Throws<CsvValidationException>(() => _sut.Parse(ToStream(csv)));
        Assert.Equal("EXCESSO_COLUNAS", ex.Code);
    }

    // ── Internal methods ─────────────────────────────────────────

    [Fact]
    public void ValidateBinaryContent_NormalText_DoesNotThrow()
    {
        var bytes = Encoding.UTF8.GetBytes("subject,description\nError,Issue");
        CsvService.ValidateBinaryContent(bytes);
    }

    [Fact]
    public void ValidateBinaryContent_TooManyNulls_Throws()
    {
        var bytes = new byte[200];
        Array.Fill(bytes, (byte)0x00);

        Assert.Throws<CsvValidationException>(() => CsvService.ValidateBinaryContent(bytes));
    }

    [Fact]
    public void DecodeText_Utf8WithoutBom_Works()
    {
        var bytes = Encoding.UTF8.GetBytes("test accentuation");
        var result = CsvService.DecodeText(bytes);
        Assert.Contains("accentuation", result);
    }

    [Fact]
    public void DecodeText_Latin1_FallbackWorks()
    {
        var bytes = Encoding.Latin1.GetBytes("café résumé");
        var result = CsvService.DecodeText(bytes);
        Assert.Contains("caf", result);
    }

    [Fact]
    public void ValidateTextContent_NormalText_DoesNotThrow()
    {
        CsvService.ValidateTextContent("subject,description\nError,Issue");
    }

    [Fact]
    public void ValidateTextContent_Empty_Throws()
    {
        Assert.Throws<CsvValidationException>(() => CsvService.ValidateTextContent("   "));
    }

    [Fact]
    public void CleanText_RemovesInvisibleCharacters()
    {
        var text = "﻿subject​";
        var result = CsvService.CleanText(text);
        Assert.Equal("subject", result);
    }

    [Fact]
    public void CleanField_RemovesNulls()
    {
        var field = "text\0with\0nulls";
        var result = CsvService.CleanField(field);
        Assert.Equal("textwithnulls", result);
    }

    [Fact]
    public void CleanField_Null_ReturnsNull()
    {
        Assert.Null(CsvService.CleanField(null));
    }

    // ── Edge cases ───────────────────────────────────────────────

    [Fact]
    public void Parse_HeaderOnlyNoData_ReturnsEmptyList()
    {
        var csv = "subject,description";
        var result = _sut.Parse(ToStream(csv));
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_HeaderWithSpaces_WorksCorrectly()
    {
        var csv = " subject , description \nError,Issue";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
    }

    [Fact]
    public void Parse_WindowsLineEndings_WorksCorrectly()
    {
        var csv = "subject,description\r\nError,Issue\r\nBug,Failure";
        var result = _sut.Parse(ToStream(csv));

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Parse_LargeNumberOfLines_Works()
    {
        var sb = new StringBuilder("subject,description\n");
        for (var i = 0; i < 1000; i++)
            sb.AppendLine($"Ticket {i},Description of ticket {i}");

        var result = _sut.Parse(ToStream(sb.ToString()));
        Assert.Equal(1000, result.Count);
    }

    [Fact]
    public void Parse_FieldsWithLineBreaks_KeepContent()
    {
        var csv = "subject,description\n\"Error\",\"Line 1\nLine 2\nLine 3\"";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
        Assert.Contains("Line 1", result[0].Description);
        Assert.Contains("Line 3", result[0].Description);
    }

    [Fact]
    public void Parse_ControlCharacters_AreHandled()
    {
        var csv = "subject,description\nError\x01\x02,Issue\x03";
        var result = _sut.Parse(ToStream(csv));

        Assert.Single(result);
    }
}
