% This is a single-line comment.
%: This is a documentation comment.
% This is another single-line comment that isn't part of the documentation.
%: TODO: List of work to be done.
todo(global, ['Unicode', 'Modules', 'Coroutining', 'Regex']).
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
'>'(A, B) :- @cmp(A > B).
'<'(A, B) :- @cmp(A < B).
'>='(A, B) :- @cmp(A >= B).
'<='(A, B) :- @cmp(A <= B).
'\\=='(A, B) :- @not(@eq(A, B)).
':='(A, B) :- @set(A, B).
'?='(A,B) :- \+@unifiable(A,B,[_|_]).
';'(A, _) :- A.
';'(_, B) :- B.

% LIST HELPERS

member(_, [], _) :- false.
member(X, [H|T], '>') :- X = H ; member(X, T, '>').
member(X, [H|T], '<') :- member(X, T, '<') ; X = H.
member(X, Y) :- member(X, Y, '>').

% MATH HELPERS

between_(Min, _, _, M) :- M is Min.
between_(Min, Max, Step, M) :-
	Min < Max,
	between_(@eval(Min + Step), Max, Step, M).

between('[', Min, Max, ']', Step, M) :- between_(Min, Max, Step, M).
between('(', Min, Max, ']', Step, M) :- between_(@eval(Min + Step), Max, Step, M).
between('[', Min, Max, ')', Step, M) :- between_(Min, @eval(Max - Step), Step, M).
between('(', Min, Max, ')', Step, M) :- between_(@eval(Min + Step), @eval(Max - Step), Step, M).
between(Min, Max, X) :- between('[', Min, Max, ')', 1, X).
