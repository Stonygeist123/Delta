using Delta.Analysis;
using System.Collections;

namespace Delta.Diagnostics
{
    internal class DiagnosticBag : IEnumerable<Diagnostic>
    {
        private readonly List<Diagnostic> _diagnostics = [];

        public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(Diagnostic diagnostic) => _diagnostics.Add(diagnostic);

        public void Add(string text, string message, TextSpan span) => _diagnostics.Add(new(text, message, span));

        public void AddAll(IEnumerable<Diagnostic> diagnostics) => _diagnostics.AddRange(diagnostics);

        public void PrintAll() => _diagnostics.ForEach(d =>
        {
            d.Print();
            Console.WriteLine();
        });
    }
}