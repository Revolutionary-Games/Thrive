// https://stackoverflow.com/q/62648189/9783687

namespace System.Runtime.CompilerServices
{
#pragma warning disable SA1135
    using ComponentModel;

#pragma warning restore SA1135

    [EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable SA1649
    internal class IsExternalInit
    {
    }
#pragma warning restore SA1649
}
