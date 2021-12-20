using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang
{
    [DebuggerDisplay("{ Explain(this) }")]
    public readonly struct Module
    {
        public readonly Atom Name;
        public readonly List Exports;
        public readonly List Imports;
        public readonly Operator[] Operators;
        public readonly bool Runtime;
        public readonly KnowledgeBase KnowledgeBase;

        public Module(Atom name, List import, List export, Operator[] operators, KnowledgeBase kb = null, bool runtime = false)
        {
            Name = name;
            Imports = import;
            Exports = export;
            Operators = operators;
            Runtime = runtime;
            KnowledgeBase = kb ?? new();
        }

        public static string Explain(Module m)
        {
            var expl = $":- module({Atom.Explain(m.Name)}, {List.Explain(m.Exports)}).";
            return expl;
        }

        public Module WithImport(Atom import) => new(Name, List.Build(Imports.Head.Contents.Append(import).ToArray()), Exports, Operators, KnowledgeBase, Runtime);
        public Module WithExports(Term[] exports) => new(Name, Imports, List.Build(exports), Operators, KnowledgeBase, Runtime);
        public Module WithOperators(Operator[] operators) => new(Name, Imports, Exports, operators, KnowledgeBase, Runtime);
    }
}
