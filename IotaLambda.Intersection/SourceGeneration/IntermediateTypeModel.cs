using Microsoft.CodeAnalysis;
using System.Text;

namespace IotaLambda.Intersection.SourceGeneration;

internal record IntermediateTypeModel
(
    string Namespace,
    bool IsGlobalNamespace,
    Accessibility DeclaredAccessibility,
    bool IsReadonly,
    TypeModel Type,
    EquatableArray<TypeComponentModel> TypeComponents,
    EquatableArray<ImplicitCastModel> ImplicitCasts
);

internal record TypeModel
(
    string Name,
    string Fqn,
    EquatableArray<TypeModel> Args
);

internal record TypeComponentModel
(
    TypeModel Type,
    EquatableArray<MethodMemberModel> MethodMembers
);

internal record MethodMemberModel
(
    bool ReturnsVoid,
    TypeModel ReturnType,
    string Name,
    EquatableArray<TypeParameterModel> TypeParameters,
    EquatableArray<ParameterModel> Parameters
)
{
    public string GetSignature()
    {
        var signatureSb = new StringBuilder();
        signatureSb.Append(Name).Append("(");
        var first = true;
        if (Parameters != default)
        {
            foreach (var p in Parameters)
            {
                if (!first)
                    signatureSb.Append(", ");
                signatureSb.AppendTypeFqn(p.Type, simpleNameForOutermostType: false);
            }
        }
        signatureSb.Append(")");
        return signatureSb.ToString();
    }
}

internal record TypeParameterModel
(
    string Name,
    bool HasReferenceTypeConstraint,
    bool HasValueTypeConstraint,
    EquatableArray<TypeModel> ConstraintTypes,
    bool HasConstructorConstraint
);

internal record ParameterModel
(
    TypeModel Type,
    NullableAnnotation NullableAnnotation,
    string Name,
    bool HasExplicitDefaultValue,
    string ExplicitDefaultValueStr
);

internal record ImplicitCastModel
(
    TypeModel Type,
    bool From,
    bool To
);