using TicketClassifier.Api.Gateways.Interface;

namespace TicketClassifier.Tests;

public class CategoriesTests
{
    [Fact]
    public void ParseBatch_ArrayJson_ReturnsResults()
    {
        var json = """[{"indice":0,"categoria":"Bug","prioridade":"High","departamento":"Development","resumo":"Login error","sentimento":"negative","tags":["login","error"],"confianca":0.9,"justificativa":"Clear failure"}]""";
        var result = Categories.ParseBatch(json);

        Assert.Single(result);
        Assert.Equal("Bug", result[0].Category);
        Assert.Equal("High", result[0].Priority);
    }

    [Fact]
    public void ParseBatch_SingleObject_ReturnsResult()
    {
        var json = """{"indice":0,"categoria":"Login","prioridade":"High","departamento":"Support","resumo":"Error","sentimento":"negative","tags":[],"confianca":0.85,"justificativa":""}""";
        var result = Categories.ParseBatch(json);

        Assert.Single(result);
        Assert.Equal("Login", result[0].Category);
    }

    [Fact]
    public void ParseBatch_ObjectWithMarkdown_ReturnsResult()
    {
        var json = "```json\n{\"indice\":0,\"categoria\":\"Bug\",\"prioridade\":\"Medium\",\"departamento\":\"Support\",\"resumo\":\"\",\"sentimento\":\"neutral\",\"tags\":[],\"confianca\":0.9,\"justificativa\":\"\"}\n```";
        var result = Categories.ParseBatch(json);

        Assert.Single(result);
        Assert.Equal("Bug", result[0].Category);
    }

    [Fact]
    public void ParseBatch_EmptyFields_UsesDefaults()
    {
        var json = """[{"indice":0,"categoria":"","prioridade":"","departamento":"","resumo":"","sentimento":"","tags":[],"confianca":0.9,"justificativa":""}]""";
        var result = Categories.ParseBatch(json);

        Assert.Single(result);
        Assert.Equal("Other", result[0].Category);
        Assert.Equal("Medium", result[0].Priority);
        Assert.Equal("Support", result[0].Department);
        Assert.Equal("neutral", result[0].Sentiment);
    }

    [Fact]
    public void ParseBatchWithFallback_CorrectIndices_UsesIndices()
    {
        var json = """[{"indice":0,"categoria":"Bug","prioridade":"High","departamento":"Development","resumo":"","sentimento":"negative","tags":[],"confianca":0.85,"justificativa":""}]""";
        var result = Categories.ParseBatchWithFallback(json, new[] { 0 });

        Assert.Single(result);
        Assert.True(result.ContainsKey(0));
        Assert.Equal("Bug", result[0].Category);
    }

    [Fact]
    public void ParseBatchWithFallback_WrongIndices_UsesPositional()
    {
        var json = """[{"indice":99,"categoria":"Login","prioridade":"High","departamento":"Support","resumo":"","sentimento":"neutral","tags":[],"confianca":0.8,"justificativa":""}]""";
        var result = Categories.ParseBatchWithFallback(json, new[] { 0 });

        Assert.Single(result);
        Assert.True(result.ContainsKey(0));
        Assert.Equal("Login", result[0].Category);
    }

    [Fact]
    public void IsFallback_DefaultFallback_ReturnsTrue()
    {
        Assert.True(Categories.IsFallback(Categories.Fallback));
    }

    [Fact]
    public void IsFallback_FallbackWithError_ReturnsTrue()
    {
        var f = Categories.FallbackWithError("Some error");
        Assert.True(Categories.IsFallback(f));
    }

    [Fact]
    public void IsFallback_RealResult_ReturnsFalse()
    {
        var r = new ClassificationResult("Bug", "High", "Development", "Summary", 0.9, "Justification", "negative", new[] { "login" });
        Assert.False(Categories.IsFallback(r));
    }

    [Fact]
    public void ValidCategory_CaseInsensitive_Recognizes()
    {
        Assert.Equal("Question", Categories.ValidCategory("question"));
        Assert.Equal("Complaint", Categories.ValidCategory("complaint"));
    }

    [Fact]
    public void ValidCategory_Invalid_ReturnsOther()
    {
        Assert.Equal("Other", Categories.ValidCategory("nonexistent"));
        Assert.Equal("Other", Categories.ValidCategory(null));
    }
}
