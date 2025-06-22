using Delta.Binding;

namespace Delta.Environment
{
    internal static class BuiltIn
    {
        public static List<BuiltInFn> Fns = [new("print", BoundType.Void, [new("text")])];
    }
}