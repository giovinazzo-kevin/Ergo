﻿using Ergo.Interpreter.Directives;
using Ergo.Solver.BuiltIns;

namespace Ergo.Interpreter.Libraries.IO;

public class IO : Library
{
    public override Atom Module => WellKnown.Modules.IO;
    public override IEnumerable<SolverBuiltIn> GetExportedBuiltins() => Enumerable.Empty<SolverBuiltIn>()
        .Append(new Write())
        .Append(new WriteCanonical())
        .Append(new WriteQuoted())
        .Append(new WriteDict())
        .Append(new WriteRaw())
        .Append(new Read())
        .Append(new ReadLine())
        .Append(new GetChar())
        .Append(new GetSingleChar())
        .Append(new PeekChar())
        ;
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => Enumerable.Empty<InterpreterDirective>()
        ;
}
