frequency_halfsteps(X, N, F) :-
	F is X * (2 ^ (1.0/12)) ^ N.