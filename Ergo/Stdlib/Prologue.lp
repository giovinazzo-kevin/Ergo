% This is a single-line comment.
%: This is a documentation comment.
% This is another single-line comment that isn't part of the documentation.
%: TODO: List of work to be done.
todo('Global', ['Modules']).
todo('Interpreter', ['Metapredicates', 'Typesystem']).
todo('Parser', ['Directives']).
todo('AST', ['Annotations']).

%: Writes a newline
nl :- @print('\n').
%: Evaluates to true if A cannot be proven true.
%: Example: X = 2, \+(X == 3) | X/2
'\\+'(A) :- @unproved(A), @cut.
%: Evaluates the numerical expression represented by B and assigns the result to A.
%: Example: X is 5 + 2 * 4 | X/13
%: Example: X is something | ERR Expected term of type Number, found: something 
'is'(A, B) :- @eval(A, B).
%: Attempts to unify A with B, and evaluates to the success of this attempt.
%: Example: [Head|Tail] = [1, 2, 3, 4] | Head/1, Tail/[2, 3, 4]
%: Example: false = true | No
'='(A, B) :- @unify(A, B).
'\\='(A, B) :- \+@unify(A, B).
%: Compares A and B for strict equality, without attempting unification.
%: Example: X = something, X == something | X/something
%: Example: [Head|Tail] == [1, 2, 3, 4] | No
'=='(A, B) :- @eq(A, B).
'\\=='(A, B) :- @not(@eq(A, B)).
%: Substitutes A with B directly and without attempting unification, even if A is ground. 
%: Example: X := 1, 1 := 2 | X/2
':='(A, B) :- @set(A, B).
%: Evaluates to true if either A or B succeeds.
%: Example: X = 2 ; X = 3 | X/2 | X/3
';'(A, _) :- A.
';'(_, B) :- B.