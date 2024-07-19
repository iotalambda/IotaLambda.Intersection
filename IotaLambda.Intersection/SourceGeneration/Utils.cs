using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

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

    public static void AppendInterfaceMemberDeclarationString(this IMethodSymbol mSbl, StringBuilder sb)
    {
        // Return type
        if (mSbl.ReturnsVoid)
            sb.Append("void");
        else
            AppendTypeString(mSbl.ReturnType, sb);
        sb.Append(" ");

        // Type parameters
        if (mSbl.TypeParameters.Length > 0)
        {
            sb.Append("<");
            var first = true;
            foreach (var t in mSbl.TypeParameters)
            {
                if (!first)
                    sb.Append(", ");
                sb.Append(t.Name);
                first = false;
            }
            sb.Append(">");
        }

        // Name
        sb.Append(mSbl.Name);

        // Parameters
        sb.Append("(");
        if (mSbl.Parameters.Length > 0)
        {
            var first = true;
            foreach (var p in mSbl.Parameters)
            {
                if (!first)
                    sb.Append(", ");
                AppendTypeString(p.Type, sb);
                if (p.NullableAnnotation == NullableAnnotation.Annotated)
                    sb.Append("?");
                sb.Append(" ");
                sb.Append(p.Name);
                first = false;
                if (p.HasExplicitDefaultValue)
                {
                    sb.Append(" = ");
                    if (p.ExplicitDefaultValue == default)
                        sb.Append("default");
                    else if (p.ExplicitDefaultValue == null)
                        sb.Append("null");
                    else
                        sb.Append(p.ExplicitDefaultValue.ToString());
                }
            }
        }
        sb.Append(")");

        // Type constraints
        foreach (var t in mSbl.TypeParameters)
        {
            if (t.HasReferenceTypeConstraint
                || t.HasValueTypeConstraint
                || t.ConstraintTypes.Length > 0
                || t.HasConstructorConstraint)
            {
                sb.Append(" where ");
                sb.Append(t.Name);
                var first = true;

                if (t.HasReferenceTypeConstraint)
                {
                    sb.Append("class");
                    first = false;
                }

                if (t.HasValueTypeConstraint)
                {
                    if (!first)
                        sb.Append(", ");
                    sb.Append("struct");
                    first = false;
                }

                foreach (var ct in t.ConstraintTypes)
                {
                    if (!first)
                        sb.Append(", ");
                    AppendTypeString(ct, sb);
                    first = false;
                }

                if (t.HasConstructorConstraint)
                {
                    if (!first)
                        sb.Append(", ");
                    sb.Append("new()");
                    first = false;
                }
            }
        }

        sb.Append(";");
    }

    public static void AppendTypeString(ITypeSymbol tSbl, StringBuilder sb)
    {
        sb.Append(tSbl.GetFullyQualifiedName());
        if (tSbl is INamedTypeSymbol ntSbl && !ntSbl.IsTupleType)
        {
            if (ntSbl.TypeArguments.Length > 0)
            {
                sb.Append("<");
                var first = true;
                foreach (var ta in ntSbl.TypeArguments)
                {
                    if (!first)
                        sb.Append(", ");
                    AppendTypeString(ta, sb);
                }
                sb.Append(">");
            }
        }
    }
}