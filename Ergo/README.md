Ergo is a lightweight and extensible Prolog implementation written entirely in C#.
It is a very young project, so it's neither ISO-compliant nor production-ready. 

It supports several advanced features, including:

- Modules (libraries)
- Last Call Optimization
- Tabling (memoization)
- Goal Expansions (macros)
- C# Data Bindings (marshalling)
- Unbounded Numeric Types

## Scope
The goal of Ergo is to empower C# applications with first-order logic.
It can be used in many different scenarios, and it is very easy to customize and extend with built-in predicates, directives and shell commands.
It supports automatic marshalling of terms to and from C# types, but it can also work with unmarshalled CLR `objects`.

## Roadmap
At the time of writing, Ergo is fully interpreted language with much room for optimization. 

For a rough roadmap, please refer to: https://github.com/users/G3Kappa/projects/1/views/1