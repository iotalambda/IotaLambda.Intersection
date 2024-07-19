using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace IotaLambda.Intersection.SourceGeneration;

[Generator]
public class SourceGenerator : IIncrementalGenerator
{
    record SInfo(
        string Fqn,
        string Namespace,
        bool IsGlobalNamespace,
        string DeclrStr,
        EquatableArray<(string Fqn, bool To, bool From)> ImplCasts,
        EquatableArray<SImplInterfInfo> Interfaces
    );

    record SImplInterfInfo(
        EquatableArray<string> Usings,
        Accessibility DeclaredAccessibility,
        string Fqn,
        string Namespace,
        bool IsGlobalNamespace,
        EquatableArray<string> MembDeclrStrs
    );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sInfos = context.SyntaxProvider
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
                    var sDeclr = ctx.TargetNode as StructDeclarationSyntax;

                    var implCasts = sSbl.GetAttributes()
                        .Where(a => a.AttributeClass.GetFullyQualifiedName() == "IotaLambda.Intersection.WithImplicitCastAttribute")
                        .Select(a => a.ConstructorArguments[0].Value as ITypeSymbol)
                        .Select(t => (
                            Type: t.GetFullyQualifiedName(),
                            To: t.Interfaces.All(sSbl.Interfaces.Contains),
                            From: sSbl.Interfaces.All(t.Interfaces.Contains)
                        ))
                        .ToArray();

                    return new SInfo(
                        Fqn: sSbl.GetFullyQualifiedName(),
                        Namespace: sSbl.ContainingNamespace.GetFullyQualifiedName(),
                        IsGlobalNamespace: sSbl.ContainingNamespace.IsGlobalNamespace,
                        DeclrStr: sDeclr.ToString(),
                        ImplCasts: new(implCasts),
                        Interfaces: new EquatableArray<SImplInterfInfo>(sSbl.Interfaces.Select(i =>
                        {
                            ct.ThrowIfCancellationRequested();

                            return new SImplInterfInfo(
                                Usings: new(i.DeclaringSyntaxReferences.SelectMany(d => d.GetSyntax(ct).SyntaxTree.GetCompilationUnitRoot(ct).Usings.Select(u => u.NamespaceOrType.ToString())).Distinct().OrderBy(x => x).ToArray()),
                                DeclaredAccessibility: i.DeclaredAccessibility,
                                Fqn: i.GetFullyQualifiedName(),
                                Namespace: i.ContainingNamespace.GetFullyQualifiedName(),
                                IsGlobalNamespace: i.ContainingNamespace.IsGlobalNamespace,
                                MembDeclrStrs: new(i.GetMembers().Where(m => !m.IsStatic).Select(m =>
                                {
                                    ct.ThrowIfCancellationRequested();
                                    return m.DeclaringSyntaxReferences.First().GetSyntax(ct).ToString();
                                }).ToArray()));
                        }).ToArray()));
                });

        context.RegisterSourceOutput(
            sInfos,
            static (spCtx, sInfo) =>
            {
                spCtx.CancellationToken.ThrowIfCancellationRequested();

                var cuStx = SyntaxFactory.CompilationUnit();
                CSharpSyntaxNode nsOrCuStx = cuStx;
                if (!sInfo.IsGlobalNamespace)
                    nsOrCuStx = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(sInfo.Namespace));

                static CSharpSyntaxNode AddMembersToNsOrCu(CSharpSyntaxNode nsOrCuStx, params MemberDeclarationSyntax[] members) => nsOrCuStx switch
                {
                    NamespaceDeclarationSyntax ns => ns.AddMembers(members),
                    CompilationUnitSyntax cu => cu.AddMembers(members),
                    _ => throw new NotSupportedException(),
                };

                var sDeclr = SyntaxFactory.ParseSyntaxTree(sInfo.DeclrStr)
                    .GetRoot(spCtx.CancellationToken)
                    .DescendantNodes()
                    .OfType<StructDeclarationSyntax>()
                    .First();

                var interfaces = sInfo.Interfaces.Select(i => new
                {
                    i.DeclaredAccessibility,
                    i.Fqn,
                    i.IsGlobalNamespace,
                    MembDeclrs = i.MembDeclrStrs.Select(m => SyntaxFactory.ParseMemberDeclaration(m)).ToArray(),
                })
                .ToArray();

                var sModifiers = sDeclr.Modifiers;
                if (!sDeclr.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                    sDeclr.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));

                var implCastDeclrs = new List<ConversionOperatorDeclarationSyntax>();
                foreach (var implCast in sInfo.ImplCasts)
                {
                    if (implCast.To)
                    {
                        var parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("source"))
                            .WithType(SyntaxFactory.ParseTypeName(sDeclr.Identifier.ToString()));
                        var parameterList = SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(parameter));
                        var returnStatement = SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(implCast.Fqn), SyntaxFactory.IdentifierName("From")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("source"))))));
                        var block = SyntaxFactory.Block(returnStatement);
                        var conversionOperator = SyntaxFactory.ConversionOperatorDeclaration(
                            SyntaxFactory.Token(SyntaxKind.ImplicitKeyword),
                            SyntaxFactory.ParseTypeName(implCast.Fqn))
                        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                        .WithParameterList(parameterList)
                        .WithBody(block);
                        implCastDeclrs.Add(conversionOperator);
                    }

                    if (implCast.From)
                    {
                        var parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("source"))
                            .WithType(SyntaxFactory.ParseTypeName(implCast.Fqn));
                        var parameterList = SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(parameter));
                        var returnStatement = SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.IdentifierName("From"),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("source"))))));
                        var block = SyntaxFactory.Block(returnStatement);
                        var conversionOperator = SyntaxFactory.ConversionOperatorDeclaration(
                            SyntaxFactory.Token(SyntaxKind.ImplicitKeyword),
                            SyntaxFactory.ParseTypeName(sDeclr.Identifier.ToString()))
                        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                        .WithParameterList(parameterList)
                        .WithBody(block);
                        implCastDeclrs.Add(conversionOperator);
                    }
                }

                nsOrCuStx = AddMembersToNsOrCu(nsOrCuStx,
                    SyntaxFactory.StructDeclaration(
                        attributeLists: default,
                        modifiers: sModifiers,
                        keyword: sDeclr.Keyword,
                        identifier: sDeclr.Identifier,
                        typeParameterList: sDeclr.TypeParameterList,
                        parameterList: sDeclr.ParameterList,
                        baseList: SyntaxFactory.BaseList().AddTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IIntersectionType"))),
                        constraintClauses: sDeclr.ConstraintClauses,
                        openBraceToken: SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                        members: SyntaxFactory.List<MemberDeclarationSyntax>()

                            .Add(SyntaxFactory.FieldDeclaration(
                                attributeLists: default,
                                modifiers: SyntaxFactory.TokenList(
                                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                    SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)),
                                declaration: SyntaxFactory.VariableDeclaration(
                                    SyntaxFactory.ParseTypeName("object"),
                                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator("s"))),
                                semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken)))

                            .Add(SyntaxFactory.ConstructorDeclaration(
                                attributeLists: default,
                                modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)),
                                identifier: sDeclr.Identifier,
                                parameterList: SyntaxFactory.ParameterList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Parameter(
                                            attributeLists: default,
                                            modifiers: default,
                                            type: SyntaxFactory.ParseTypeName("object"),
                                            identifier: SyntaxFactory.Identifier("s"),
                                            @default: default))),
                                initializer: default,
                                body: SyntaxFactory.Block(
                                    SyntaxFactory.ExpressionStatement(
                                        SyntaxFactory.AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.ThisExpression(),
                                                SyntaxFactory.IdentifierName("s")),
                                            SyntaxFactory.IdentifierName("s")))),
                                expressionBody: default,
                                semicolonToken: default))

                            .Add(SyntaxFactory.ConstructorDeclaration(
                                attributeLists: SyntaxFactory.SingletonList(
                                    SyntaxFactory.AttributeList().AddAttributes(
                                        SyntaxFactory.Attribute(
                                            SyntaxFactory.ParseName("Obsolete"),
                                            SyntaxFactory.AttributeArgumentList().AddArguments(
                                                SyntaxFactory.AttributeArgument(
                                                    SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                                                        SyntaxFactory.Literal("Do not use the parameterless constructor."))),
                                                SyntaxFactory.AttributeArgument(
                                                    nameEquals: default,
                                                    nameColon: SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("error")),
                                                    expression: SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)))))),
                                modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                                identifier: sDeclr.Identifier,
                                parameterList: SyntaxFactory.ParameterList(),
                                initializer: default,
                                body: SyntaxFactory.Block(
                                    SyntaxFactory.ThrowStatement(
                                        SyntaxFactory.ObjectCreationExpression(
                                            type: SyntaxFactory.ParseTypeName("InvalidOperationException"),
                                            argumentList: SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                                                            SyntaxFactory.Literal("Do not use the parameterless constructor."))))),
                                            initializer: default))),
                                expressionBody: default))

                            .Add(SyntaxFactory.MethodDeclaration(
                                attributeLists: default,
                                modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)),
                                returnType: SyntaxFactory.ParseTypeName(sDeclr.Identifier.ToString()),
                                explicitInterfaceSpecifier: default,
                                identifier: SyntaxFactory.Identifier("From"),
                                typeParameterList: SyntaxFactory.TypeParameterList(
                                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.TypeParameter("T"))),
                                parameterList: SyntaxFactory.ParameterList().AddParameters(
                                    SyntaxFactory.Parameter(
                                        attributeLists: default,
                                        modifiers: default,
                                        type: SyntaxFactory.ParseTypeName("T"),
                                        identifier: SyntaxFactory.Identifier("source"),
                                        @default: default),
                                    SyntaxFactory.Parameter(
                                        attributeLists: default,
                                        modifiers: default,
                                        type: SyntaxFactory.ParseTypeName("object"),
                                        identifier: SyntaxFactory.Identifier("_"),
                                        @default: SyntaxFactory.EqualsValueClause(
                                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword))))),
                                constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>()
                                    .Add(SyntaxFactory.TypeParameterConstraintClause("T")
                                        .AddConstraints(SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint))
                                        .AddConstraints(interfaces.Select(i => SyntaxFactory.TypeConstraint(SyntaxFactory.ParseTypeName(i.Fqn))).ToArray())),
                                body: SyntaxFactory.Block(
                                    SyntaxFactory.ReturnStatement(
                                        SyntaxFactory.ObjectCreationExpression(
                                            type: SyntaxFactory.ParseTypeName(sDeclr.Identifier.ToString()),
                                            argumentList: SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("source")))),
                                            initializer: default))),
                                semicolonToken: default))

                            .Add(SyntaxFactory.MethodDeclaration(
                                attributeLists: default,
                                modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)),
                                returnType: SyntaxFactory.ParseTypeName(sDeclr.Identifier.ToString()),
                                explicitInterfaceSpecifier: default,
                                identifier: SyntaxFactory.Identifier("From"),
                                typeParameterList: SyntaxFactory.TypeParameterList(
                                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.TypeParameter("T"))),
                                parameterList: SyntaxFactory.ParameterList().AddParameters(
                                    SyntaxFactory.Parameter(
                                        attributeLists: default,
                                        modifiers: default,
                                        type: SyntaxFactory.ParseTypeName("T"),
                                        identifier: SyntaxFactory.Identifier("source"),
                                        @default: default)),
                                constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>()
                                    .Add(SyntaxFactory.TypeParameterConstraintClause("T")
                                        .AddConstraints(SyntaxFactory.ClassOrStructConstraint(SyntaxKind.StructConstraint))
                                        .AddConstraints(SyntaxFactory.TypeConstraint(SyntaxFactory.ParseTypeName("IIntersectionType")))
                                        .AddConstraints(interfaces.Select(i => SyntaxFactory.TypeConstraint(SyntaxFactory.ParseTypeName(i.Fqn))).ToArray())),
                                body: SyntaxFactory.Block(
                                    SyntaxFactory.ReturnStatement(
                                        SyntaxFactory.ObjectCreationExpression(
                                            type: SyntaxFactory.ParseTypeName(sDeclr.Identifier.ToString()),
                                            argumentList: SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.InvocationExpression(
                                                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("Helpers"), SyntaxFactory.IdentifierName("S")),
                                                            SyntaxFactory.ArgumentList(
                                                                SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("source")))))))),
                                            initializer: default))),
                                expressionBody: default,
                                semicolonToken: default))

                            .AddRange(interfaces
                                .SelectMany(i => i.MembDeclrs.OfType<MethodDeclarationSyntax>().Select(m => (i, m, s: m.GetSignature())))
                                .GroupBy(x => x.s)
                                .Select(g => g.FirstOrDefault())
                                .Where(x => x.s != null)
                                .Select(x => SyntaxFactory.MethodDeclaration(
                                    attributeLists: default,
                                    modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                                    returnType: x.m.ReturnType,
                                    explicitInterfaceSpecifier: x.m.ExplicitInterfaceSpecifier,
                                    identifier: x.m.Identifier,
                                    typeParameterList: x.m.TypeParameterList,
                                    parameterList: x.m.ParameterList,
                                    constraintClauses: x.m.ConstraintClauses,
                                    body: SyntaxFactory.Block(
                                        x.m.ReturnType.ToString() == "void"
                                            ? SyntaxFactory.ExpressionStatement(
                                                SyntaxFactory.InvocationExpression(
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.ParenthesizedExpression(
                                                            SyntaxFactory.BinaryExpression(
                                                                SyntaxKind.AsExpression,
                                                                SyntaxFactory.MemberAccessExpression(
                                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                                    SyntaxFactory.ThisExpression(),
                                                                    SyntaxFactory.IdentifierName("s")),
                                                                SyntaxFactory.ParseTypeName(x.i.Fqn))),
                                                        SyntaxFactory.IdentifierName(x.m.Identifier)),
                                                    SyntaxFactory.ArgumentList(
                                                        SyntaxFactory.SeparatedList<ArgumentSyntax>().AddRange(x.m.ParameterList?.Parameters.Select(p =>
                                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(p.Identifier))) ?? []))))
                                            : SyntaxFactory.ReturnStatement(
                                                SyntaxFactory.InvocationExpression(
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.ParenthesizedExpression(
                                                            SyntaxFactory.BinaryExpression(
                                                                SyntaxKind.AsExpression,
                                                                SyntaxFactory.MemberAccessExpression(
                                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                                    SyntaxFactory.ThisExpression(),
                                                                    SyntaxFactory.IdentifierName("s")),
                                                                SyntaxFactory.ParseTypeName(x.i.Fqn))),
                                                        SyntaxFactory.IdentifierName(x.m.Identifier)),
                                                    SyntaxFactory.ArgumentList(
                                                        SyntaxFactory.SeparatedList<ArgumentSyntax>().AddRange(x.m.ParameterList?.Parameters.Select(p =>
                                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(p.Identifier))) ?? []))))),
                                    semicolonToken: default)))

                            .AddRange(interfaces
                                .Select(i => SyntaxFactory.MethodDeclaration(
                                    attributeLists: default,
                                    modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                                    returnType: SyntaxFactory.ParseTypeName(i.Fqn),
                                    explicitInterfaceSpecifier: default,
                                    identifier: SyntaxFactory.Identifier("As"),
                                    typeParameterList: SyntaxFactory.TypeParameterList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.TypeParameter("T"))),
                                    parameterList: SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Parameter(
                                        attributeLists: default,
                                        modifiers: default,
                                        type: SyntaxFactory.ParseTypeName(i.Fqn),
                                        identifier: SyntaxFactory.Identifier("_"),
                                        @default: SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))))),
                                    constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>()
                                        .Add(SyntaxFactory.TypeParameterConstraintClause("T")
                                        .AddConstraints(SyntaxFactory.TypeConstraint(SyntaxFactory.ParseTypeName(i.Fqn)))),
                                    body: SyntaxFactory.Block(
                                        SyntaxFactory.ReturnStatement(
                                            SyntaxFactory.BinaryExpression(
                                                SyntaxKind.AsExpression,
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.ThisExpression(),
                                                    SyntaxFactory.IdentifierName("s")),
                                                SyntaxFactory.ParseTypeName(i.Fqn)))),
                                    expressionBody: default,
                                    semicolonToken: default)))

                            .AddRange(implCastDeclrs)

                            .Add(SyntaxFactory.MethodDeclaration(
                                attributeLists: default,
                                modifiers: SyntaxFactory.TokenList(),
                                returnType: SyntaxFactory.ParseTypeName("object"),
                                explicitInterfaceSpecifier: SyntaxFactory.ExplicitInterfaceSpecifier(SyntaxFactory.IdentifierName("IIntersectionType")),
                                identifier: SyntaxFactory.Identifier("S"),
                                typeParameterList: default,
                                parameterList: SyntaxFactory.ParameterList(),
                                constraintClauses: default,
                                body: SyntaxFactory.Block(
                                    SyntaxFactory.ReturnStatement(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.ThisExpression(),
                                            SyntaxFactory.IdentifierName("s")))),
                                expressionBody: default,
                                semicolonToken: default)),


                        closeBraceToken: SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                        semicolonToken: default));

                cuStx = nsOrCuStx switch
                {
                    NamespaceDeclarationSyntax ns => cuStx.AddMembers(ns),
                    CompilationUnitSyntax cu => cu,
                    _ => throw new NotSupportedException(),
                };

                var usings = new List<string>
                {
                    "System",
                    "IotaLambda.Intersection.Internal"
                };

                foreach (var i in sInfo.Interfaces)
                {
                    if (!i.IsGlobalNamespace)
                        usings.Add(i.Namespace);
                    usings.AddRange(i.Usings);
                }

                cuStx = cuStx
                    .AddUsings(usings.Distinct().Select(u => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(u))).ToArray())
                    .NormalizeWhitespace();

                spCtx.AddSource($"{sInfo.Fqn}.g.cs", SourceText.From(cuStx.ToFullString(), Encoding.UTF8));
            });
    }
}
