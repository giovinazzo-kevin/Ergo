dude(kappa).
dude(miskude).
dude(gold).

likes(kappa, genre(roguelike)).
likes(kappa, genre(roguelite)).
likes(kappa, genre('arena FPS')).
likes(kappa, genre(fighting)).

likes(miskude, genre(roguelite)).
likes(miskude, genre('arena FPS')).
likes(miskude, genre(racing)).
likes(miskude, genre(fighting)).

likes(gold, genre('visual novel')).
likes(gold, genre(fighting)).
likes(gold, genre(racing)).
likes(gold, genre(metaprogression)).

game(roguelike, 'Dungeon Crawl Stone Soup').
game(roguelite, 'Caveblazers').
game('arena FPS', 'Serious Sam').
game(fighting, 'Tekken 7').
game(racing, 'Trackmania').
game('visual novel', 'Higurashi When They Cry').
game(metaprogression, 'Deep Rock Galactic').

common_game(A, B, G) :-
	dude(A),
	dude(B),
	@not(@eq(A, B)),
	likes(A, genre(_Genre)),
	likes(B, genre(_Genre)),
	game(_Genre, G).