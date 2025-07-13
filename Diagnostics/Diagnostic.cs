using Delta.Analysis;
using System.Collections;

namespace Delta.Diagnostics
{
    internal class Diagnostic(TextLocation? location, string message)
    {
        public TextLocation? Location { get; } = location;
        public string Message { get; } = message;

        public override string ToString() => Message;
    }

    internal sealed class DiagnosticBag : IEnumerable<Diagnostic>
    {
        private readonly List<Diagnostic> _diagnostics = [];

        public void Report(TextLocation location, string message) => _diagnostics.Add(new(location, message));

        public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void AddRange(DiagnosticBag diagnostics) => _diagnostics.AddRange(diagnostics);

        public void AddRange(IEnumerable<Diagnostic> diagnostics) => _diagnostics.AddRange(diagnostics);
    }
}