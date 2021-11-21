frequency_halfsteps(X, N, F) :-
	F is X * (2 ^ (1.0/12)) ^ N.
	
note(+0 , 'A').
note(+1 , 'A♯').
note(+2 , 'B').
note(+3 , 'C').
note(+4 , 'C♯').
note(+5 , 'D').
note(+6 , 'D♯').
note(+7 , 'E').
note(+8 , 'F').
note(+9 , 'F♯').
note(+10, 'G').
note(+11, 'G♯').

note(-0 , 'A').
note(-1 , 'G♭').
note(-2 , 'G').
note(-3 , 'F♭').
note(-4 , 'F').
note(-5 , 'E').
note(-6 , 'D♭').
note(-7 , 'D').
note(-8 , 'C♭').
note(-9 , 'C').
note(-10, 'B').
note(-11, 'A♭').