using TicketClassifier.Api.Gateways.Interface;

namespace TicketClassifier.Tests;

public class CategoriasTests
{
    [Fact]
    public void ParseLote_ArrayJson_RetornaResultados()
    {
        var json = """[{"indice":0,"categoria":"Bug","prioridade":"Alta","departamento":"Desenvolvimento","resumo":"Erro no login","sentimento":"negativo","tags":["login","erro"],"confianca":0.9,"justificativa":"Falha clara"}]""";
        var result = Categorias.ParseLote(json);

        Assert.Single(result);
        Assert.Equal("Bug", result[0].Categoria);
        Assert.Equal("Alta", result[0].Prioridade);
    }

    [Fact]
    public void ParseLote_ObjetoUnico_RetornaResultado()
    {
        var json = """{"indice":0,"categoria":"Login","prioridade":"Alta","departamento":"Suporte","resumo":"Erro","sentimento":"negativo","tags":[],"confianca":0.85,"justificativa":""}""";
        var result = Categorias.ParseLote(json);

        Assert.Single(result);
        Assert.Equal("Login", result[0].Categoria);
    }

    [Fact]
    public void ParseLote_ObjetoComMarkdown_RetornaResultado()
    {
        var json = "```json\n{\"indice\":0,\"categoria\":\"Bug\",\"prioridade\":\"Média\",\"departamento\":\"Suporte\",\"resumo\":\"\",\"sentimento\":\"neutro\",\"tags\":[],\"confianca\":0.9,\"justificativa\":\"\"}\n```";
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
        Assert.Equal("Outro", result[0].Categoria);
        Assert.Equal("Média", result[0].Prioridade);
        Assert.Equal("Suporte", result[0].Departamento);
        Assert.Equal("neutro", result[0].Sentimento);
    }

    [Fact]
    public void ParseLoteComFallback_IndicesCorretos_UsaIndices()
    {
        var json = """[{"indice":0,"categoria":"Bug","prioridade":"Alta","departamento":"Desenvolvimento","resumo":"","sentimento":"negativo","tags":[],"confianca":0.85,"justificativa":""}]""";
        var result = Categorias.ParseLoteComFallback(json, new[] { 0 });

        Assert.Single(result);
        Assert.True(result.ContainsKey(0));
        Assert.Equal("Bug", result[0].Categoria);
    }

    [Fact]
    public void ParseLoteComFallback_IndicesErrados_UsaPosicional()
    {
        var json = """[{"indice":99,"categoria":"Login","prioridade":"Alta","departamento":"Suporte","resumo":"","sentimento":"neutro","tags":[],"confianca":0.8,"justificativa":""}]""";
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
        var f = Categorias.FallbackComErro("Erro qualquer");
        Assert.True(Categorias.EhFallback(f));
    }

    [Fact]
    public void EhFallback_ResultadoReal_RetornaFalse()
    {
        var r = new ClassificacaoResultado("Bug", "Alta", "Desenvolvimento", "Resumo", 0.9, "Justificativa", "negativo", new[] { "login" });
        Assert.False(Categorias.EhFallback(r));
    }

    [Fact]
    public void CategoriaValida_ComAcento_Reconhece()
    {
        Assert.Equal("Dúvida", Categorias.CategoriaValida("duvida"));
        Assert.Equal("Reclamação", Categorias.CategoriaValida("reclamacao"));
    }

    [Fact]
    public void CategoriaValida_Invalida_RetornaOutro()
    {
        Assert.Equal("Outro", Categorias.CategoriaValida("inexistente"));
        Assert.Equal("Outro", Categorias.CategoriaValida(null));
    }
}
