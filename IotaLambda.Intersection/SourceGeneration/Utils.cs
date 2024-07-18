using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IotaLambda.Intersection.SourceGeneration;

internal static class Utils
{
    static readonly SymbolDisplayFormat FullyQualifiedNameSymbolDisplayFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
    public static string GetFullyQualifiedName(this ITypeSymbol sbl) => sbl.ToDisplayString(FullyQualifiedNameSymbolDisplayFormat);

    public static string GetSignature(this MethodDeclarationSyntax mDeclr)
    {
        return $"{mDeclr.Identifier}({string.Join(", ", mDeclr.ParameterList?.Parameters.Select(p => p.Type.ToString()) ?? [])})";
    }
}