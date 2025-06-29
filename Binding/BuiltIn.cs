using Delta.Symbols;

namespace Delta.Binding
{
    internal static class BuiltIn
    {
        public static List<BuiltInFn> Fns = [new("print", TypeSymbol.Void, [new("text", TypeSymbol.Any)])];
    }
}