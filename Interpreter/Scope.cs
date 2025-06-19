namespace Delta.Interpreter
{
    internal class Scope(Scope? parent)
    {
        private readonly Dictionary<string, object> _variables = [];
        public Dictionary<string, object> Variables => _variables;
        public Scope? Parent { get; } = parent;

        public bool TryDeclareVar(string name, object value)
        {
            if (HasVar(name))
                return false;
            _variables.Add(name, value);
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
    }
}