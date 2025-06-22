using Delta.Binding.BoundNodes;

namespace Delta.Binding
{
    internal abstract class BoundSymbol(string name, BoundType type)
    {
        public string Name { get; } = name;
        public BoundType Type { get; } = type;
    }

    internal class VarSymbol(string name, BoundType type, bool mutable) : BoundSymbol(name, type)
    {
        public bool Mutable { get; } = mutable;
    }

    internal class ParamSymbol(string name) : VarSymbol(name, BoundType.Number, true)
    {
    }

    internal class FnSymbol(string name, BoundType type, List<ParamSymbol> paramList, BoundBlockStmt? body) : BoundSymbol(name, type)
    {
        public List<ParamSymbol> ParamList { get; } = paramList;
        public BoundBlockStmt? Body { get; } = body;
    }

    internal class BuiltInFn(string name, BoundType type, List<ParamSymbol> paramList) : FnSymbol(name, type, paramList, null)
    {
    }
}