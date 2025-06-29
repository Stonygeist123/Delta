using Delta.Analysis.Nodes;
using Delta.IO;

namespace Delta.Symbols
{
    internal static class SymbolPrinter
    {
        public static void WriteTo(this Symbol symbol, TextWriter writer)
        {
            switch (symbol)
            {
                case ParamSymbol p:
                    writer.WriteIdentifier(p.Name);
                    writer.WritePunctuation(NodeKind.Colon);
                    writer.WriteSpace();
                    p.Type.WriteTo(writer);
                    break;

                case VarSymbol v:
                    writer.WriteKeyword(NodeKind.Var);
                    writer.WriteSpace();
                    if (v.Mutable)
                    {
                        writer.WriteKeyword(NodeKind.Mut);
                        writer.WriteSpace();
                    }

                    writer.WriteIdentifier(v.Name);
                    writer.WritePunctuation(NodeKind.Colon);
                    writer.WriteSpace();
                    v.Type.WriteTo(writer);
                    break;

                case TypeSymbol t:
                    writer.WriteIdentifier(t.Name);
                    break;

                case FnSymbol f:
                    writer.WriteKeyword(NodeKind.Fn);
                    writer.WriteSpace();
                    writer.WriteIdentifier(symbol.Name);
                    writer.WritePunctuation(NodeKind.LParen);
                    for (int i = 0; i < f.Parameters.Length; ++i)
                    {
                        if (i > 0)
                        {
                            writer.WritePunctuation(NodeKind.Comma);
                            writer.WriteSpace();
                        }

                        f.Parameters[i].WriteTo(writer);
                    }

                    writer.WritePunctuation(NodeKind.RParen);
                    writer.WriteSpace();
                    writer.WritePunctuation(NodeKind.Arrow);
                    writer.WriteSpace();
                    f.Type.WriteTo(writer);
                    break;

                case LabelSymbol:
                    break;

                default:
                    break;
            }
        }
    }
}