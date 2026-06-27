// Polyfill for System.Runtime.CompilerServices.IsExternalInit, which is
// required by C# 9 `init` accessors but is missing on netstandard2.0 and
// net48. Reference: https://stackoverflow.com/a/64749403
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif