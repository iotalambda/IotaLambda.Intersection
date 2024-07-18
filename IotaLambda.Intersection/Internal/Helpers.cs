using System.ComponentModel;

namespace IotaLambda.Intersection.Internal;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class Helpers
{
    public static object S<T>(T source) where T : struct, IIntersectionType
    {
        return source.S();
    }
}
