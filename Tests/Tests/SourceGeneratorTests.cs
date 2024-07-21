using FluentAssertions;

namespace Tests;

public class SourceGeneratorTests
{
    [Fact]
    public void BasicHappyCase_Ok()
    {
        var sources = new List<string>();
        sources.Add("""
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

        sources.Add("""
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

    [Fact]
    public void OverlappingComponents_Ok()
    {
        var sources = new List<string>();

        sources.Add("""
            using IotaLambda.Intersection;

            interface IComponent1
            {
                void Method1();
                void OverlappingMethod();
            }

            interface IComponent2
            {
                void Method2();
                void OverlappingMethod();
            }

            [IntersectionType]
            readonly partial struct SComposition : IComponent1, IComponent2;
            """);

        sources.Add("""
            using System;

            class Implementation : IComponent1, IComponent2
            {
                public void Method1() => Console.WriteLine("Method1 Invoked");
                public void Method2() => Console.WriteLine("Method2 Invoked");
                public void OverlappingMethod() => Console.WriteLine("OverlappingMethod Invoked");
            }
            """);

        sources.Add("""
            var composition = SComposition.From(new Implementation());
            composition.Method1();
            composition.Method2();
            composition.OverlappingMethod();
            """);

        TestRunning.Run([.. sources]).Should().BeEquivalentTo(
            [
                "Method1 Invoked",
                "Method2 Invoked",
                "OverlappingMethod Invoked"
            ]);
    }

    [Fact]
    public void MethodWithReturnTypes_Ok()
    {
        var sources = new List<string>();

        sources.Add("""
            using System.Threading.Tasks;
            using IotaLambda.Intersection;

            interface IComponent1
            {
                Task<MyReturnType> MethodWithReturnType();
            }

            class MyReturnType
            {
                public string Value { get; set; }
            }

            [IntersectionType]
            readonly partial struct SComposition : IComponent1;
            """);

        sources.Add("""
            using System.Threading.Tasks;

            class Implementation : IComponent1
            {
                public Task<MyReturnType> MethodWithReturnType() => Task.FromResult(new MyReturnType { Value = "MyReturnType Value" });
            }
            """);

        sources.Add("""
            using System;

            var composition = SComposition.From(new Implementation());
            var result = composition.MethodWithReturnType();
            Console.WriteLine(result.Result.Value);
            """);

        TestRunning.Run([.. sources]).Should().BeEquivalentTo(
            [
                "MyReturnType Value"
            ]);
    }

    [Fact]
    public void MethodWithParameters_Ok()
    {
        var sources = new List<string>();

        sources.Add("""
            using System.Threading.Tasks;
            using IotaLambda.Intersection;

            interface IComponent1
            {
                void MethodWithParameters(Task<MyParamType> param1, string param2 = "MyDefault");
            }

            class MyParamType
            {
                public string Value { get; set; }
            }

            [IntersectionType]
            readonly partial struct SComposition : IComponent1;
            """);

        sources.Add("""
            using System;
            using System.Threading.Tasks;

            class Implementation : IComponent1
            {
                public void MethodWithParameters(Task<MyParamType> param1, string param2 = "MyDefault")
                {
                    Console.WriteLine($"param1 = {param1.Result.Value}");
                    Console.WriteLine($"param2 = {param2}");
                }
            }
            """);

        sources.Add("""
            using System.Threading.Tasks;

            var composition = SComposition.From(new Implementation());
            composition.MethodWithParameters(Task.FromResult(new MyParamType { Value = "MyParamType Value" }));
            """);

        TestRunning.Run([.. sources]).Should().BeEquivalentTo(
            [
                "param1 = MyParamType Value",
                "param2 = MyDefault",
            ]);
    }
}