% This is a single-line comment.
%: This is a documentation comment.
% This is another single-line comment that isn't part of the documentation.
%: TODO: List of work to be done.
%: (TODO: refactor into a proper list lol)
todo('Global', ['Modules']).
todo('Interpreter', ['Metapredicates', 'Typesystem']).
todo('Parser', ['Directives']).
todo('AST', ['Prefix/Infix/Postfix Operators', 'Annotations']).

%: Writes a newline
nl :- @print('\n').

add(A, B, C) :- @set(C, @math_add(A, B)).

map([], []) :- @cut.
map([X], [Y]) :- 
	add(1, X, Y).
map('[|]'(Head, Rest), '[|]'(RetHead, RetRest)) :- 
	@not(@eq(Rest, [])),
	map([Head], [RetHead]), 
	map(Rest, RetRest).