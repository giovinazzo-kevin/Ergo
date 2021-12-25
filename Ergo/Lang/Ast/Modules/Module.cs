using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang.Ast
{
    [DebuggerDisplay("{ Explain() }")]
    public readonly struct Module
    {
        public readonly Atom Name;
        public readonly List Exports;
        public readonly List Imports;
        public readonly Operator[] Operators;
        public readonly ErgoProgram Program;
        public readonly bool Runtime;

        public Module(Atom name, List import, List export, Operator[] operators, ErgoProgram program, bool runtime = false)
        {
            Name = name;
            Imports = import;
            Exports = export;
            Operators = operators;
            Program = program;
            Runtime = runtime;
        }

        public string Explain()
        {
            var expl = $":- module({Name.Explain()}, {Exports.Explain()}).";
            return expl;
        }

        public Module WithImport(Atom import) => new(Name, new(Imports.Contents.Append(import).ToArray()), Exports, Operators, Program, Runtime);
        public Module WithExports(ITerm[] exports) => new(Name, Imports, new(exports), Operators, Program, Runtime);
        public Module WithOperators(Operator[] operators) => new(Name, Imports, Exports, operators, Program, Runtime);
        public Module WithOperator(Operator op) => new(Name, Imports, Exports, Operators.Append(op).ToArray(), Program, Runtime);
        public Module WithProgram(ErgoProgram p) => new(Name, Imports, Exports, Operators, p, Runtime);
    }
}
