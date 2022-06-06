using Ergo.Shell;
using Raylib_cs;
using Builtins;
using Ergo.Lang.Ast;
using Ergo.Lang;
using Ergo.Lang.Extensions;
using Color = Raylib_cs.Color;

var uiModule = new Atom("ui");

var shell = new ErgoShell(configureSolver: solver => {
    solver.TryAddBuiltIn(new fps());
    solver.TryAddBuiltIn(new blit());
    solver.TryAddBuiltIn(new canvas());
    solver.TryAddBuiltIn(new origin());
    solver.TryAddBuiltIn(new mouse());
}, configureInterpreter: interpreter =>
{
    
});
var scope = shell.CreateScope();
shell.Load(ref scope, "example");

scope = scope.WithInterpreterScope(scope.InterpreterScope
    .WithModule(scope.InterpreterScope.Modules[uiModule]
        .WithDynamicPredicate(new Complex(new Atom("draw"), new Variable("_")).GetSignature())
        .WithDynamicPredicate(new Atom("init").GetSignature())));

var uiThread = new Thread(() =>
{
    var uiScope = scope
        .WithExceptionThrowing(true);
    var solver = shell.CreateSolver(ref uiScope);
    var query = new Query(new(new Atom("init")));
    foreach (var sol in solver.Solve(query)) ;
    Raylib.InitWindow(canvas.Value.Width, canvas.Value.Height, "Hello World");
    Raylib.SetTargetFPS(fps.Value);
    while (!Raylib.WindowShouldClose())
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.WHITE);
        query = new(new(new Complex(new("draw"), new Complex(new("time"), new Atom(Raylib.GetTime())))));
        foreach(var sol in solver.Solve(query))
        {
            foreach (var call in Render.Calls)
            {
                call();
            }
            Render.Calls.Clear();
        }
        Raylib.EndDrawing();
    }
    Raylib.CloseWindow();
});

uiThread.Start();
shell.EnterRepl(ref scope, str => Raylib.WindowShouldClose() || str.Trim().Equals("exit"));
uiThread.Join();

public static class Render
{
    public static readonly List<Action> Calls = new();
}