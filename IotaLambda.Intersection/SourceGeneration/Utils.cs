using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IotaLambda.Intersection.SourceGeneration;

internal static class Utils
{
    static readonly SymbolDisplayFormat FullyQualifiedNameSymbolDisplayFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static string GetFullyQualifiedName(this ISymbol sbl) => sbl.ToDisplayString(FullyQualifiedNameSymbolDisplayFormat);

    public static string GetSignature(this MethodDeclarationSyntax mDeclr)
    {
        return $"{mDeclr.Identifier}({string.Join(", ", mDeclr.ParameterList?.Parameters.Select(p => p.Type.ToString()) ?? [])})";
    }
}