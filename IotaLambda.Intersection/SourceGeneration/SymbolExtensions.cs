using Microsoft.CodeAnalysis;

namespace IotaLambda.Intersection.SourceGeneration;

internal static class SymbolExtensions
{
    static readonly SymbolDisplayFormat FullyQualifiedNameSymbolDisplayFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static string GetFullyQualifiedName(this ISymbol symbol) => symbol.ToDisplayString(FullyQualifiedNameSymbolDisplayFormat);

    public static TypeModel GetTypeModel(this ISymbol symbol)
    {
        TypeModel[] args = default;
        if (symbol is INamedTypeSymbol ntSbl && !ntSbl.IsTupleType && ntSbl.TypeArguments.Length > 0)
            args = ntSbl.TypeArguments.Select(ta => ta.GetTypeModel()).ToArray();

        return new TypeModel(
            symbol.Name,
            symbol.GetFullyQualifiedName(),
            new(args ?? []));
    }
}
