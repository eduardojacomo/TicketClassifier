using TicketClassifier.Api.Gateways.Interface;

namespace TicketClassifier.Tests;

public class CategoriasTests
{
    [Fact]
    public void ParseLote_ArrayJson_RetornaResultados()
    {
        var json = """[{"indice":0,"categoria":"Bug","prioridade":"High","departamento":"Development","resumo":"Login error","sentimento":"negative","tags":["login","error"],"confianca":0.9,"justificativa":"Clear failure"}]""";
        var result = Categorias.ParseLote(json);

        Assert.Single(result);
        Assert.Equal("Bug", result[0].Categoria);
        Assert.Equal("High", result[0].Prioridade);
    }

    [Fact]
    public void ParseLote_ObjetoUnico_RetornaResultado()
    {
        var json = """{"indice":0,"categoria":"Login","prioridade":"High","departamento":"Support","resumo":"Error","sentimento":"negative","tags":[],"confianca":0.85,"justificativa":""}""";
        var result = Categorias.ParseLote(json);

        Assert.Single(result);
        Assert.Equal("Login", result[0].Categoria);
    }

    [Fact]
    public void ParseLote_ObjetoComMarkdown_RetornaResultado()
    {
        var json = "```json\n{\"indice\":0,\"categoria\":\"Bug\",\"prioridade\":\"Medium\",\"departamento\":\"Support\",\"resumo\":\"\",\"sentimento\":\"neutral\",\"tags\":[],\"confianca\":0.9,\"justificativa\":\"\"}\n```";
        var result = Categorias.ParseLote(json);

        Assert.Single(result);
        Assert.Equal("Bug", result[0].Categoria);
    }

    [Fact]
    public void ParseLote_CamposVazios_UsaDefaults()
    {
        var json = """[{"indice":0,"categoria":"","prioridade":"","departamento":"","resumo":"","sentimento":"","tags":[],"confianca":0.9,"justificativa":""}]""";
        var result = Categorias.ParseLote(json);

        Assert.Single(result);
        Assert.Equal("Other", result[0].Categoria);
        Assert.Equal("Medium", result[0].Prioridade);
        Assert.Equal("Support", result[0].Departamento);
        Assert.Equal("neutral", result[0].Sentimento);
    }

    [Fact]
    public void ParseLoteComFallback_IndicesCorretos_UsaIndices()
    {
        var json = """[{"indice":0,"categoria":"Bug","prioridade":"High","departamento":"Development","resumo":"","sentimento":"negative","tags":[],"confianca":0.85,"justificativa":""}]""";
        var result = Categorias.ParseLoteComFallback(json, new[] { 0 });

        Assert.Single(result);
        Assert.True(result.ContainsKey(0));
        Assert.Equal("Bug", result[0].Categoria);
    }

    [Fact]
    public void ParseLoteComFallback_IndicesErrados_UsaPosicional()
    {
        var json = """[{"indice":99,"categoria":"Login","prioridade":"High","departamento":"Support","resumo":"","sentimento":"neutral","tags":[],"confianca":0.8,"justificativa":""}]""";
        var result = Categorias.ParseLoteComFallback(json, new[] { 0 });

        Assert.Single(result);
        Assert.True(result.ContainsKey(0));
        Assert.Equal("Login", result[0].Categoria);
    }

    [Fact]
    public void EhFallback_FallbackPadrao_RetornaTrue()
    {
        Assert.True(Categorias.EhFallback(Categorias.Fallback));
    }

    [Fact]
    public void EhFallback_FallbackComErro_RetornaTrue()
    {
        var f = Categorias.FallbackComErro("Some error");
        Assert.True(Categorias.EhFallback(f));
    }

    [Fact]
    public void EhFallback_ResultadoReal_RetornaFalse()
    {
        var r = new ClassificacaoResultado("Bug", "High", "Development", "Summary", 0.9, "Justification", "negative", new[] { "login" });
        Assert.False(Categorias.EhFallback(r));
    }

    [Fact]
    public void CategoriaValida_CaseInsensitive_Reconhece()
    {
        Assert.Equal("Question", Categorias.CategoriaValida("question"));
        Assert.Equal("Complaint", Categorias.CategoriaValida("complaint"));
    }

    [Fact]
    public void CategoriaValida_Invalida_RetornaOutro()
    {
        Assert.Equal("Other", Categorias.CategoriaValida("nonexistent"));
        Assert.Equal("Other", Categorias.CategoriaValida(null));
    }
}
