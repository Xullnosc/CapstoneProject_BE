using FluentAssertions;
using Services.AI.Text;

namespace FCTMS.Tests.Services.AI.Text;

public class TfIdfServiceTests
{
    private readonly ITfIdfService _service = new TfIdfService();

    [Fact]
    public void BuildModel_ShouldCreateVectors_ForEveryDocument()
    {
        var docs = new Dictionary<string, string>
        {
            ["target"] = "deep learning model for image classification",
            ["candidate-a"] = "image classification with deep neural network",
            ["candidate-b"] = "distributed database transaction consistency"
        };

        var model = _service.BuildModel(docs);

        model.Vectors.Should().HaveCount(3);
        model.Vectors["target"].NormalizedWeights.Should().NotBeEmpty();
    }

    [Fact]
    public void GetTopKSimilar_ShouldRankClosestDocumentFirst()
    {
        var docs = new Dictionary<string, string>
        {
            ["target"] = "ai based thesis plagiarism detection using nlp",
            ["candidate-a"] = "nlp plagiarism detection for thesis content",
            ["candidate-b"] = "campus parking lot management with iot sensors",
            ["candidate-c"] = "thesis duplicate checking using ai and nlp"
        };

        var model = _service.BuildModel(docs);

        var top = _service.GetTopKSimilar("target", model, k: 2);

        top.Should().HaveCount(2);
        top[0].Score.Should().BeGreaterThan(top[1].Score);
        top.Select(x => x.DocumentId).Should().Contain("candidate-a").And.Contain("candidate-c");
    }

    [Fact]
    public void BuildModel_ShouldRespectMaxDocumentFrequencyRatio()
    {
        var docs = new Dictionary<string, string>
        {
            ["d1"] = "common uniqueone",
            ["d2"] = "common uniquetwo",
            ["d3"] = "common uniquethree"
        };

        var model = _service.BuildModel(docs, new TfIdfOptions { MaxDocumentFrequencyRatio = 0.66d });

        model.Vocabulary.Keys.Should().NotContain("common");
        model.Vocabulary.Keys.Should().Contain(new[] { "uniqueone", "uniquetwo", "uniquethree" });
    }

    [Fact]
    public void CosineSimilarity_ShouldReturnZero_WhenNoSharedTerms()
    {
        var docs = new Dictionary<string, string>
        {
            ["a"] = "computer vision segmentation",
            ["b"] = "financial auditing compliance"
        };

        var model = _service.BuildModel(docs);

        var score = _service.CosineSimilarity(model.Vectors["a"], model.Vectors["b"]);

        score.Should().Be(0d);
    }

    [Fact]
    public void BuildModel_WithCapstoneProfile_ShouldRemoveTemplateWords()
    {
        var docs = new Dictionary<string, string>
        {
            ["d1"] = "capstone project register objectives sprint backlog",
            ["d2"] = "capstone project register objectives jwt rbac"
        };

        var model = _service.BuildModel(docs, new TfIdfOptions
        {
            DomainProfile = TfIdfOptions.DomainProfileCapstone
        });

        model.Vocabulary.Keys.Should().NotContain(new[] { "capstone", "register", "objectives" });
        model.Vocabulary.Keys.Should().Contain(new[] { "sprint", "backlog", "jwt", "rbac" });
    }

    [Fact]
    public void BuildModel_WithUnknownDomainProfile_ShouldThrow()
    {
        var docs = new Dictionary<string, string>
        {
            ["d1"] = "alpha beta",
            ["d2"] = "beta gamma"
        };

        Action act = () => _service.BuildModel(docs, new TfIdfOptions { DomainProfile = "unknown-profile" });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BuildModel_WithCapstoneProfile_ShouldDownweightGenericTechTerms()
    {
        var docs = new Dictionary<string, string>
        {
            ["d1"] = "react alpha",
            ["d2"] = "angular beta"
        };

        var model = _service.BuildModel(docs, new TfIdfOptions
        {
            DomainProfile = TfIdfOptions.DomainProfileCapstone
        });

        var d1Weights = model.Vectors["d1"].NormalizedWeights;
        var reactTermId = model.Vocabulary["react"];
        var alphaTermId = model.Vocabulary["alpha"];

        d1Weights[reactTermId].Should().BeLessThan(d1Weights[alphaTermId]);
    }
}