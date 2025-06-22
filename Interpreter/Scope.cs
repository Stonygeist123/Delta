using Delta.Binding.BoundNodes;

namespace Delta.Interpreter
{
    internal class Scope(Scope? parent)
    {
        private readonly Dictionary<string, object> _variables = [];
        private readonly Dictionary<string, BoundBlockStmt> _fns = [];
        public Dictionary<string, object> Variables => _variables;
        public Dictionary<string, BoundBlockStmt> Fns => _fns;
        public Scope? Parent { get; } = parent;

        public bool TryDeclareVar(string name, object value)
        {
            if (HasVar(name))
                return false;
            _variables.Add(name, value);
            return true;
        }

        public bool TryDeclareFn(string name, BoundBlockStmt value)
        {
            if (HasFn(name))
                return false;
            _fns.Add(name, value);
            return true;
        }

        public bool TryGetVar(string name, out object? value) =>
            _variables.TryGetValue(name, out value) || (Parent is not null && Parent.TryGetVar(name, out value));

        public bool TryAssign(string name, object value)
        {
            if (Variables.ContainsKey(name))
            {
                _variables[name] = value;
                return true;
            }

            return Parent is not null && Parent.TryAssign(name, value);
        }

        public bool HasVar(string name) =>
            _variables.ContainsKey(name) || (Parent is not null && Parent.HasVar(name));

        public bool HasFn(string name) =>
            _fns.ContainsKey(name) || (Parent is not null && Parent.HasFn(name));
    }
}