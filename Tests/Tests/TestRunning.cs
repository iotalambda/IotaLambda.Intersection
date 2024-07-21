using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

namespace Tests;

static class TestRunning
{
    public static string[] Run(
        string[] sources,
        bool noGeneratorDiagnostics = true,
        bool noGeneratedDiagnostics = true)
    {

        Compilation comp = CSharpCompilation.Create("compilation",
            sources.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray(),
            new[]
            {
                MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Private.CoreLib, Version=8.0.0.0").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=8.0.0.0").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Console, Version=8.0.0.0").Location),
                MetadataReference.CreateFromFile(typeof(IotaLambda.Intersection.IntersectionTypeAttribute).GetTypeInfo().Assembly.Location),
            },
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new IotaLambda.Intersection.SourceGeneration.SourceGenerator());
        driver.RunGeneratorsAndUpdateCompilation(comp, out var compWithGeneratorsRun, out var diag);

        if (noGeneratorDiagnostics)
            diag.Should().BeEmpty();

        if (noGeneratedDiagnostics)
            compWithGeneratorsRun.GetDiagnostics().Should().BeEmpty();

        Assembly assembly;
        using (var ms = new MemoryStream())
        {
            var emitResult = compWithGeneratorsRun.Emit(ms);
            ms.Seek(0, SeekOrigin.Begin);
            assembly = Assembly.Load(ms.ToArray());
        }

        string stdOut;
        using (var sw = new StringWriter())
        {
            var originalOut = Console.Out;
            try
            {
                Console.SetOut(sw);
                assembly.EntryPoint.Invoke(null, [Array.Empty<string>()]);
                stdOut = sw.ToString();
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        return stdOut.Split(Environment.NewLine).SkipLast(1).ToArray();
    }
}
