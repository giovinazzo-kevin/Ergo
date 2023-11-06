## Design Goals
Ergo brings first-order logic to the .NET world through a lightweight and extensible Prolog implementation written entirely in C#. It is a relatively young project, so it's neither ISO-compliant nor stable, but it's been consistently improving over the past few years. 

Its main design goals are to be flexible and customizable, to handle interop with C# seamlessly, and to be efficient enough to be worthwhile as a scripting language in high-demand applications such as games.
Thanks to its versatile syntax and extensible architecture, Ergo can be adapted to any use case and lends itself well to the creation of domain-specific languages. 
Unification allows for very complex pattern-matching, and users can even implement their own parsers for their own *abstract types* that override standard unification, or add their own built-ins.

Ergo already supports several advanced features, including:

- Libraries (C# entry points for various Ergo extensions; linked to Ergo modules)
- Tail Call Optimization (for the execution of tail recursive predicates)
- Inlining (a pre-processing step that expands occurrences of a predicate's head into a disjunction of its clauses)
  - While not as important for compiled prolog, this step achieves measurable performance gains in the interpreted realm
- Predicate Expansions (macros/term rewriting)
- Tabling (memoization)
- Abstract Terms & Abstract Term Parsers (for custom types implemented on top of canonical terms)
    - Dictionaries (akin to SWI-Prolog)
    - Ordered Sets
    - Lists
    - Tuples (comma-lists)
- Marshalling of CLR objects to/from Ergo terms (both complex-style and dictionary-style)
- Unbounded Numeric Types (i.e. BigDecimal as the underlying numeric type)
    - In the future, optimizations for integer and float arithmetic could be added, but performance-critical codepaths can be delegated to C#
- Lambdas & Higher-Kinded Predicates 
- Dynamic Predicates

Initially a purely interpreted language, Ergo is now (optionally) compiled down to IL. This enables all sorts of optimizations, and makes Ergo competitive with native C# code in cases where the interpreter overhead would be a deal-breaker.
In fact, Ergo comes with three _execution modes_, each an improvement over the previous one:
- Interpreted: the slowest, but safest mode. Implements recursive backtracking through a IEnumerable interface and has to resolve each goal through the knowledge base every time.
- Executed: a good mix between Interpreted and Compiled. Performs static analysis on a query or hook and compiles them to an intermediate execution graph that can be optionally further optimized.
  - Built-ins can optimize their own execution graphs, sometimes even optimizing themselves away in the process.
  - This is where, forr instance, optimizations that propagate constant unifications or remove dead unifications are performed.
  - The interface is otherwise similar to that exposed by the Interpreted mode, and goals that can't be optimized will be called through the interpreter.
- Compiled: the fastest, but least safe mode. Takes an optimized execution graph and compiles it down to IL. Uses a stack-based virtual machine instead of the usual IEnumerable-like interface.
  - Built-ins can emit their own IL. If they don't, then a virtual call to a wrapper that calls them in interprerted mode will be generated.
    - This is usually fine, especially if the built-in doesn't call dynamic goals. But now the most heavily-used built-ins, like `unify/2` can generate their own IL to optimize away the enumerator state machine.
  - Dynamic goals will still be resolved in interpreted mode through a wrapper much like built-ins that don't emit their own IL.

## Roadmap
At the time of writing, Ergo is a ~~fully interpreted~~ **partially compiled** toy language with much room for optimization. 

For a rough roadmap, please refer to: https://github.com/users/G3Kappa/projects/1/views/1
