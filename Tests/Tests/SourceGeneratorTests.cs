using FluentAssertions;

namespace Tests;

public class SourceGeneratorTests
{
    [Fact]
    public void BasicHappyCase_Ok()
    {
        var sources = new List<string>();
        sources.Add($$$"""
            using IotaLambda.Intersection;

            interface IComponent1
            {
                void Method1();
            }

            interface IComponent2
            {
                void Method2();
            }

            [IntersectionType]
            readonly partial struct SComposition : IComponent1, IComponent2;
            """);

        sources.Add($$$"""
            using System;

            class Implementation : IComponent1, IComponent2
            {
                public void Method1() => Console.WriteLine("Method1 Invoked");
                public void Method2() => Console.WriteLine("Method2 Invoked");
            }
            """);

        sources.Add("""
            var composition = SComposition.From(new Implementation());
            composition.Method1();
            composition.Method2();
            """);

        TestRunning.Run([.. sources]).Should().BeEquivalentTo(
            [
                "Method1 Invoked",
                "Method2 Invoked"
            ]);
    }
}