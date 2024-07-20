using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace IotaLambda.Intersection.SourceGeneration;

[Generator]
public class SourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var intermediateTypeModels = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "IotaLambda.Intersection.IntersectionTypeAttribute",
                predicate: static (stx, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    return stx is StructDeclarationSyntax;
                },
                transform: static (ctx, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    var sSbl = ctx.TargetSymbol as ITypeSymbol;

                    return new IntermediateTypeModel(
                        Namespace: sSbl.ContainingNamespace.GetFullyQualifiedName(),
                        IsGlobalNamespace: sSbl.ContainingNamespace.IsGlobalNamespace,
                        DeclaredAccessibility: sSbl.DeclaredAccessibility,
                        IsReadonly: sSbl.IsReadOnly,
                        Type: sSbl.GetTypeModel(),
                        TypeComponents: new EquatableArray<TypeComponentModel>(sSbl.AllInterfaces.Select(i =>
                        {
                            ct.ThrowIfCancellationRequested();

                            return new TypeComponentModel(
                                Type: i.GetTypeModel(),
                                MethodMembers: new(i.GetMembers().Where(m => !m.IsStatic).OfType<IMethodSymbol>().Select(m =>
                                {
                                    ct.ThrowIfCancellationRequested();
                                    return new MethodMemberModel
                                    (
                                        ReturnsVoid: m.ReturnsVoid,
                                        ReturnType: m.ReturnType.GetTypeModel(),
                                        Name: m.Name,
                                        TypeParameters: new(m.TypeParameters.Select(tp =>
                                        {
                                            ct.ThrowIfCancellationRequested();
                                            return new TypeParameterModel
                                            (
                                                Name: tp.Name,
                                                HasReferenceTypeConstraint: tp.HasReferenceTypeConstraint,
                                                HasValueTypeConstraint: tp.HasValueTypeConstraint,
                                                ConstraintTypes: new(tp.ConstraintTypes.Select(cty =>
                                                {
                                                    ct.ThrowIfCancellationRequested();
                                                    return cty.GetTypeModel();
                                                }).ToArray()),
                                                HasConstructorConstraint: tp.HasConstructorConstraint
                                            );
                                        }).ToArray()),
                                        Parameters: new(m.Parameters.Select(p =>
                                        {
                                            ct.ThrowIfCancellationRequested();
                                            return new ParameterModel
                                            (
                                                Type: p.Type.GetTypeModel(),
                                                NullableAnnotation: p.NullableAnnotation,
                                                Name: p.Name,
                                                HasExplicitDefaultValue: p.HasExplicitDefaultValue,
                                                ExplicitDefaultValueStr:
                                                    p.HasExplicitDefaultValue
                                                    ? p.ExplicitDefaultValue == default ? "default"
                                                        : p.ExplicitDefaultValue == null ? "null"
                                                        : p.ExplicitDefaultValue.ToString()
                                                    : null
                                            );
                                        }).ToArray())
                                    );
                                }).ToArray()));
                        }).ToArray()),
                        ImplicitCasts: new(sSbl.GetAttributes()
                            .Where(a => a.AttributeClass.GetFullyQualifiedName() == "IotaLambda.Intersection.WithImplicitCastAttribute")
                            .Select(a => a.ConstructorArguments[0].Value as ITypeSymbol)
                            .Select(t => new ImplicitCastModel
                            (
                                Type: t.GetTypeModel(),
                                To: t.AllInterfaces.All(sSbl.AllInterfaces.Contains),
                                From: sSbl.AllInterfaces.All(t.AllInterfaces.Contains)
                            ))
                            .ToArray())
                    );
                });

        context.RegisterSourceOutput(
            intermediateTypeModels,
            static (spCtx, intermediateTypeModel) =>
            {
                spCtx.CancellationToken.ThrowIfCancellationRequested();

                var sb = new StringBuilder();
                sb.AppendIntermediateTypeModel(intermediateTypeModel);
                var str = sb.ToString();
                sb = null;
                var cuStx = SyntaxFactory.ParseCompilationUnit(str).NormalizeWhitespace();
                str = null;

                sb = new StringBuilder();
                sb.AppendTypeFqn(intermediateTypeModel.Type, false).Append(".g.cs");
                var fileName = sb.ToString().Replace(" ", "").Replace("<", "_").Replace(">", "__").Replace(",", "___");
                spCtx.AddSource(fileName, SourceText.From(cuStx.ToFullString(), Encoding.UTF8));
            });
    }
}
