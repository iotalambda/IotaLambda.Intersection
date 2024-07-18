namespace IotaLambda.Intersection;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public class WithImplicitCastAttribute(Type other) : Attribute
{
    public Type Other { get; } = other;
}