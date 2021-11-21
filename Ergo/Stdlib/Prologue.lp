% This is a single-line comment.
%: This is a documentation comment.
% This is another single-line comment that isn't part of the documentation.
%: TODO: List of work to be done.
todo(global, ['Modules', 'Coroutining', 'Regex']).
todo(csharp, ['Generators', 'CLR Attributes']).
todo(interpreter, ['Metapredicates', 'User-defined operators', 'Basic typesystem']).
todo(parser, ['Directives']).
todo(ast, ['Annotations']).

nl :- @print('\n').

% OPERATOR DECLARATIONS

'\\+'(A) :- @unprovable(A), @cut.
'is'(A, B) :- @eval(A, B).
'='(A, B) :- @unify(A, B).
'\\='(A, B) :- \+(A = B).
'=='(A, B) :- @eq(A, B).
'\\=='(A, B) :- @not(@eq(A, B)).
':='(A, B) :- @set(A, B).
'?='(A,B) :- \+@unifiable(A,B,[_|_]).
';'(A, _) :- A.
';'(_, B) :- B.

