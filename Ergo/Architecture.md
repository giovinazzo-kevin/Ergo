# Architecture

```mermaid
%%{init: { "flowchart": { "curve": "basis" } } }%%
graph LR;

RawText((Source)) --> Lexer


subgraph Libraries
BuiltIns(BuiltIns)
Directives(Directives)
end

subgraph Parser
direction LR
Lexer
end


BuiltIns .-o KnowledgeBase
Libraries .-o Interpreter
Directives --> InterpreterScope

Parser .-o Interpreter;

subgraph Interpreter
direction LR
end

subgraph Shell
direction TB
Commands(Commands)
subgraph ShellScope
end
end


KnowledgeBase .-o VirtualMachine

subgraph VirtualMachine
direction TB
subgraph Compiler

end
Ops(Ops)
Compiler --> Ops
end


InterpreterScope-->KnowledgeBase;

Interpreter .-o Shell;


Commands <--> ShellScope

Shell --> VirtualMachine
Query .-o VirtualMachine

VirtualMachine --> Solutions((Solutions))
```

The shell is the top-level interactive Ergo environment.
It manages parsing, interpreting, compiling, debugging and executing code through its various commands.

The interpreter is the fulcrum of the architecture. It is necessary in order to create a knowledge base, and it can be used without a shell.
It's extended through libraries, which export built-ins and directives, and handle Ergo events.

The virtual machine is the engine that's actually responsible for answering queries against the knowledge base.
It also handles compilation, though most of it actually happens automatically through events.

All layers of the architecture can be extended by implementing the corresponding Command, Directive, BuiltIn or Op.

# New Architecture

