% This is a single-line comment.
%: This is a documentation comment.
% This is another single-line comment that isn't part of the documentation.
%: TODO: List of work to be done.
todo('Global', ['Modules']).
todo('Interpreter', ['Metapredicates', 'Typesystem']).
todo('Parser', ['Directives']).
todo('AST', ['Annotations']).

nl :- @print('\n').
'\\+'(A) :- @unprovable(A), @cut.
'is'(A, B) :- @eval(A, B).
'='(A, B) :- @unify(A, B).
'\\='(A, B) :- \+@unify(A, B).
'=='(A, B) :- @eq(A, B).
'\\=='(A, B) :- @not(@eq(A, B)).
':='(A, B) :- @set(A, B).
'?='(A,B) :- \+(@unifiable(A,B,S), S \= []).
';'(A, _) :- A.
';'(_, B) :- B.