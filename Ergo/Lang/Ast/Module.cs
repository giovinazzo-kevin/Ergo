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
        public readonly KnowledgeBase KnowledgeBase;

        public Module(Atom name, List import, List export, KnowledgeBase kb = null)
        {
            Name = name;
            Imports = import;
            Exports = export;
            KnowledgeBase = kb ?? new();
        }

        public static string Explain(Module m)
        {
            var expl = $":- module({Atom.Explain(m.Name)}, {List.Explain(m.Exports)}).";
            return expl;
        }

        public Module WithImport(Atom import) => new(Name, List.Build(Imports.Head.Contents.Append(import).ToArray()), Exports, KnowledgeBase);
    }
}
