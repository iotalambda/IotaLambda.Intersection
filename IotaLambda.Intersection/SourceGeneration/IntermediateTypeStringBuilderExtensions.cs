using Microsoft.CodeAnalysis;
using System.Text;

namespace IotaLambda.Intersection.SourceGeneration;

internal static class IntermediateTypeStringBuilderExtensions
{
    public static StringBuilder AppendIntermediateTypeModel(this StringBuilder sb, IntermediateTypeModel model)
    {
        if (!model.IsGlobalNamespace)
        {
            sb.Append("namespace ").Append(model.Namespace).AppendLine();
            sb.Append("{").AppendLine();
        }
        {
            sb.AppendAccessibility(model.DeclaredAccessibility, trailingSpace: true);
            if (model.IsReadonly)
                sb.Append("readonly ");

            sb.Append("partial struct ").AppendTypeFqn(model.Type, simpleNameForOutermostType: true)
                .Append(" : IotaLambda.Intersection.Internal.IIntersectionType")
                .AppendLine();

            sb.Append("{").AppendLine();
            {
                sb.Append("private readonly object s;").AppendLine();

                sb.AppendLine();

                sb.Append("private ").Append(model.Type.Name).Append("(object s)").AppendLine();
                sb.Append("{").AppendLine();
                sb.Append("this.s = s;").AppendLine();
                sb.Append("}").AppendLine();

                sb.AppendLine();

                sb.Append("[System.Obsolete(\"Do not use the parameterless constructor.\", error: true)]").AppendLine();
                sb.Append("public ").Append(model.Type.Name).Append("()").AppendLine();
                sb.Append("{").AppendLine();
                sb.Append("throw new System.InvalidOperationException(\"Do not use the parameterless constructor.\");").AppendLine();
                sb.Append("}").AppendLine();

                sb.AppendLine();

                sb.Append("public static ").AppendTypeFqn(model.Type, simpleNameForOutermostType: true).Append(" From<T>(T source, object _ = default) where T : class");
                foreach (var tc in model.TypeComponents)
                    sb.Append(", ").AppendTypeFqn(tc.Type, simpleNameForOutermostType: false);
                sb.AppendLine();
                sb.Append("{").AppendLine();
                sb.Append("return new ").AppendTypeFqn(model.Type, simpleNameForOutermostType: true).Append("(source);").AppendLine();
                sb.Append("}").AppendLine();

                sb.AppendLine();

                sb.Append("public static ").AppendTypeFqn(model.Type, simpleNameForOutermostType: true).Append(" From<T>(T source) where T : struct, IotaLambda.Intersection.Internal.IIntersectionType");
                foreach (var tc in model.TypeComponents)
                    sb.Append(", ").AppendTypeFqn(tc.Type, simpleNameForOutermostType: false);
                sb.AppendLine();
                sb.Append("{").AppendLine();
                sb.Append("return new ").AppendTypeFqn(model.Type, simpleNameForOutermostType: true).Append("(IotaLambda.Intersection.Internal.Helpers.S(source));").AppendLine();
                sb.Append("}").AppendLine();

                sb.AppendLine();

                var addedSignatures = new HashSet<string>();
                foreach (var tc in model.TypeComponents)
                    foreach (var m in tc.MethodMembers)
                    {
                        var s = m.GetSignature();
                        if (!addedSignatures.Add(s))
                            continue;
                        sb.Append("public ").AppendDeclarationSignature(m).AppendLine();
                        sb.Append("{").AppendLine();
                        if (!m.ReturnsVoid)
                            sb.Append("return ");
                        sb.Append("(this.s as ").AppendTypeFqn(tc.Type, false).Append(").").Append(m.Name).Append("(");
                        {
                            var first = true;
                            foreach (var a in m.Parameters)
                            {
                                if (!first)
                                    sb.Append(", ");
                                sb.Append(a.Name);
                                first = false;
                            }
                        }
                        sb.Append(");").AppendLine();
                        sb.Append("}").AppendLine();

                        sb.AppendLine();
                    }

                foreach (var tc in model.TypeComponents)
                {
                    sb.Append("public ").AppendTypeFqn(tc.Type, simpleNameForOutermostType: false).Append(" AsComponent").Append("<T>(")
                        .AppendTypeFqn(tc.Type, simpleNameForOutermostType: false).Append(" _ = default) where T : ").AppendTypeFqn(tc.Type, simpleNameForOutermostType: false)
                        .AppendLine();
                    sb.Append("{").AppendLine();
                    sb.Append("return this.s as ").AppendTypeFqn(tc.Type, simpleNameForOutermostType: false).Append(";").AppendLine();
                    sb.Append("}").AppendLine();

                    sb.AppendLine();
                }

                if (model.ImplicitCasts != default)
                {
                    foreach (var ic in model.ImplicitCasts)
                    {
                        if (ic.From)
                        {
                            sb.Append("public static implicit operator ").AppendTypeFqn(model.Type, simpleNameForOutermostType: true)
                                .Append("(").AppendTypeFqn(ic.Type, simpleNameForOutermostType: false).Append(" source)").AppendLine();
                            sb.Append("{").AppendLine();
                            sb.Append("return From(source);").AppendLine();
                            sb.Append("}").AppendLine();

                            sb.AppendLine();
                        }

                        if (ic.To)
                        {
                            sb.Append("public static implicit operator ").AppendTypeFqn(ic.Type, simpleNameForOutermostType: false)
                                .Append("(").AppendTypeFqn(model.Type, simpleNameForOutermostType: true).Append(" source)").AppendLine();
                            sb.Append("{").AppendLine();
                            sb.Append("return ").AppendTypeFqn(ic.Type, simpleNameForOutermostType: false).Append(".From(source);").AppendLine();
                            sb.Append("}").AppendLine();

                            sb.AppendLine();
                        }
                    }
                }

                sb.Append("object IotaLambda.Intersection.Internal.IIntersectionType.S()").AppendLine();
                sb.Append("{").AppendLine();
                sb.Append("return this.s;").AppendLine();
                sb.Append("}").AppendLine();
            }
            sb.Append("}").AppendLine();
        }
        if (!model.IsGlobalNamespace)
            sb.Append("}").AppendLine();

        return sb;
    }

    public static StringBuilder AppendAccessibility(this StringBuilder sb, Accessibility accessibility, bool trailingSpace)
    {
        switch (accessibility)
        {
            case Accessibility.Public:
                sb.Append("public");
                break;
            case Accessibility.Private:
                sb.Append("private");
                break;
            case Accessibility.Internal:
                sb.Append("internal");
                break;
            case Accessibility.Protected:
                sb.Append("protected");
                break;
            case Accessibility.ProtectedAndInternal:
                sb.Append("protected internal");
                break;
            default: return sb;
        }

        if (trailingSpace)
            sb.Append(" ");
        return sb;
    }

    public static StringBuilder AppendTypeFqn(this StringBuilder sb, TypeModel type, bool simpleNameForOutermostType)
    {
        if (simpleNameForOutermostType)
            sb.Append(type.Name);
        else
            sb.Append(type.Fqn);

        if (type.Args != default && type.Args.Count > 0)
        {
            sb.Append("<");
            bool first = true;
            foreach (var arg in type.Args)
            {
                if (!first)
                    sb.Append(", ");
                sb.AppendTypeFqn(arg, simpleNameForOutermostType: false);
                first = false;
            }
            sb.Append(">");
        }

        return sb;
    }

    public static StringBuilder AppendDeclarationSignature(this StringBuilder sb, MethodMemberModel methodMember)
    {
        if (methodMember.ReturnsVoid)
            sb.Append("void ");
        else
            sb.AppendTypeFqn(methodMember.ReturnType, simpleNameForOutermostType: false).Append(" ");

        sb.Append(methodMember.Name);

        if (methodMember.TypeParameters.Count > 0)
        {
            sb.Append("<");
            var first = true;
            foreach (var tp in methodMember.TypeParameters)
            {
                if (!first)
                    sb.Append(", ");
                sb.Append(tp.Name);
                first = false;
            }
            sb.Append(">");
        }

        sb.Append("(");
        if (methodMember.Parameters.Count > 0)
        {
            var first = true;
            foreach (var p in methodMember.Parameters)
            {
                if (!first)
                    sb.Append(", ");
                sb.AppendTypeFqn(p.Type, simpleNameForOutermostType: false);
                if (p.NullableAnnotation == NullableAnnotation.Annotated)
                    sb.Append("?");
                sb.Append(" ");
                sb.Append(p.Name);
                if (p.HasExplicitDefaultValue)
                    sb.Append(" = ").Append(p.ExplicitDefaultValueStr);
                first = false;
            }
        }
        sb.Append(")");

        return sb;
    }
}
