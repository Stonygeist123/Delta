using Delta.Symbols;

namespace Delta.Binding
{
    internal static class BuiltIn
    {
        public static readonly KeyValuePair<BuiltInFn, Func<List<object?>, object?>> _printFn = new(new("print", TypeSymbol.Void, [new("text", TypeSymbol.Any)]), static args =>
        {
            Console.WriteLine(args[0]);
            return null;
        });

        public static readonly KeyValuePair<BuiltInFn, Func<List<object?>, object?>> _randomFn = new(new("random", TypeSymbol.Number, []), static args =>
        {
            Random random = new();
            return random.NextDouble();
        });

        public static Dictionary<BuiltInFn, Func<List<object?>, object?>> Fns => typeof(BuiltIn)
            .GetFields()
            .Select(p => (KeyValuePair<BuiltInFn, Func<List<object?>, object?>>)p.GetValue(null)!).ToDictionary();
    }
}