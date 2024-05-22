namespace Ergo.Lang.Compiler;

// test(a, b, f(X, Y)) 

// A1 | "test"
// A2 | "a"
// A3 | "b"
// A4 | "f"
// .. | 
// V1 | "X"
// V2 | "Y"
// .. | 
// C1 | A1 A2 A3 C2
// C2 | A4 V1 V2

public interface ITermAddress { uint Index { get; } }
