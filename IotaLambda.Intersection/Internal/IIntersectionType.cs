using System.ComponentModel;

namespace IotaLambda.Intersection.Internal;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IIntersectionType
{
    object S();
}