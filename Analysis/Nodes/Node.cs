namespace Delta.Analysis.Nodes
{
    internal abstract class Node
    {
        public abstract NodeKind Kind { get; }
        public abstract TextSpan Span { get; }
    }
}