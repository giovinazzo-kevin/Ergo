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

map([], []) :- @cut.
map([X|[]], [Y|[]]) :- Y is (X + 1), @cut.
map([X|XT], [Y|YT]) :- @not(XT = []), map([X], [Y]), map(XT, YT).