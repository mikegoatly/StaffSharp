using StaffSharp.Audio.Pipeline;
using StaffSharp.Audio.Diagnostics;
using System.Globalization;

namespace StaffSharp.Audio.Tests.Pipeline;

public class PipelineTests
{
    [Fact]
    public void PipelineContext_WithoutDiagnostics_EmitDoesNotThrow()
    {
        var context = new PipelineContext();

        // Should not throw - diagnostics are disabled
        context.EmitDiagnostics("TestStage", "testKey", 42);
        context.EmitDiagnostics("TestStage", "testKey", () => 42);
    }

    [Fact]
    public void PipelineContext_WithDiagnostics_CollectsValues()
    {
        var collector = new InMemoryDiagnosticsCollector();
        var context = new PipelineContext(collector);

        context.EmitDiagnostics("Stage1", "key1", 42);
        context.EmitDiagnostics("Stage1", "key2", "test");
        context.EmitDiagnostics("Stage2", "key1", 3.14);

        var diagnostics = collector.GetDiagnostics();

        Assert.Equal(2, diagnostics.Count);
        Assert.Equal(42, diagnostics["Stage1"]["key1"]);
        Assert.Equal("test", diagnostics["Stage1"]["key2"]);
        Assert.Equal(3.14, diagnostics["Stage2"]["key1"]);
    }

    [Fact]
    public void PipelineContext_LazyEvaluation_OnlyComputesWhenDiagnosticsEnabled()
    {
        var contextWithoutDiagnostics = new PipelineContext();
        var contextWithDiagnostics = new PipelineContext(new InMemoryDiagnosticsCollector());

        var computeCount = 0;
        int ExpensiveComputation()
        {
            computeCount++;
            return 42;
        }

        // Without diagnostics: should NOT compute
        contextWithoutDiagnostics.EmitDiagnostics("Test", "key", ExpensiveComputation);
        Assert.Equal(0, computeCount);

        // With diagnostics: should compute
        contextWithDiagnostics.EmitDiagnostics("Test", "key", ExpensiveComputation);
        Assert.Equal(1, computeCount);
    }

    [Fact]
    public void PipelineContext_NullValueFactory_Throws()
    {
        var context = new PipelineContext(new InMemoryDiagnosticsCollector());

        Assert.Throws<ArgumentNullException>(() =>
            context.EmitDiagnostics<int>("Test", "key", null!));
    }

    [Fact]
    public void AudioPipeline_SimplePipeline_ExecutesStages()
    {
        var stage1 = new TestStage<int, string>(x => x.ToString(CultureInfo.InvariantCulture));
        var stage2 = new TestStage<string, int>(x => x.Length);

        var context = new PipelineContext();
        var result = AudioPipeline.Create(42)
            .AddStage(stage1)
            .AddStage(stage2)
            .Execute(context);

        Assert.Equal(2, result); // "42".Length == 2
    }

    [Fact]
    public void AudioPipeline_WithDiagnostics_CollectsFromAllStages()
    {
        var collector = new InMemoryDiagnosticsCollector();
        var context = new PipelineContext(collector);

        var stage1 = new DiagnosticTestStage<int, int>("Stage1", x => x * 2);
        var stage2 = new DiagnosticTestStage<int, int>("Stage2", x => x + 10);

        var result = AudioPipeline.Create(5)
            .AddStage(stage1)
            .AddStage(stage2)
            .Execute(context);

        Assert.Equal(20, result); // (5 * 2) + 10 = 20

        var diagnostics = collector.GetDiagnostics();
        Assert.Equal(2, diagnostics.Count);
        Assert.Contains("Stage1", diagnostics.Keys);
        Assert.Contains("Stage2", diagnostics.Keys);
    }

    [Fact]
    public void AudioPipeline_CreateWithFactory_UsesPipelineContext()
    {
        var collector = new InMemoryDiagnosticsCollector();
        var context = new PipelineContext(collector);

        var pipeline = AudioPipeline.Create<int>(ctx =>
        {
            ctx.EmitDiagnostics("Factory", "initialized", true);
            return 100;
        }).Build();

        var result = pipeline(context);

        Assert.Equal(100, result);
        Assert.True((bool)collector.GetDiagnostics()["Factory"]["initialized"]);
    }

    [Fact]
    public void AudioPipeline_Build_ReturnsFunctionThatCanBeReused()
    {
        var stage = new TestStage<int, int>(x => x * 2);
        var pipeline = AudioPipeline.Create(10)
            .AddStage(stage)
            .Build();

        var context1 = new PipelineContext();
        var context2 = new PipelineContext();

        var result1 = pipeline(context1);
        var result2 = pipeline(context2);

        Assert.Equal(20, result1);
        Assert.Equal(20, result2);
    }

    // Test helper stages
    private sealed class TestStage<TIn, TOut> : IPipelineStage<TIn, TOut>
    {
        private readonly Func<TIn, TOut> _transform;

        public TestStage(Func<TIn, TOut> transform)
        {
            _transform = transform;
        }

        public string StageName => "TestStage";

        public TOut Process(TIn input, PipelineContext context)
        {
            return _transform(input);
        }
    }

    private sealed class DiagnosticTestStage<TIn, TOut> : IPipelineStage<TIn, TOut>
    {
        private readonly string _stageName;
        private readonly Func<TIn, TOut> _transform;

        public DiagnosticTestStage(string stageName, Func<TIn, TOut> transform)
        {
            _stageName = stageName;
            _transform = transform;
        }

        public string StageName => _stageName;

        public TOut Process(TIn input, PipelineContext context)
        {
            var output = _transform(input);
            context.EmitDiagnostics(StageName, "input", input!);
            context.EmitDiagnostics(StageName, "output", output!);
            return output;
        }
    }
}
