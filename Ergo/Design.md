# Design goals

Ergo brings logic programming to the .NET world through an opinionated Prolog implementation that targets the CLR.
Its main design goal is that of extending the C# language by providing an inference engine that's easy to work with, while being efficient enough to be used in high-demand applications such as games.
This focus on inter-operability means that Ergo is extremely easy to extend and tailor to each use case, at the cost of having a reduced feature set.

Example applications include:
	- A scripting language for a game that organically "grows" into several domain specific languages for tasks such as layouting, rendering, enemy AI, level generation, etc.
		- Ergo code is naturally data-driven, a paradigm that's particularly fitting for describing large, detailed systems such as those found in games; 
		- The combination of an extensible syntax and a module system allows for very simple representations of otherwise complex structures at any scale;
		- The serialization facilities of Ergo can be leveraged to store game data inside knowledge bases, making binary/JSON/XML serialization redundant;
		- The inference engine of Ergo lets you query and reason about the aforementioned data in a way that's orthogonal and complementary to idiomatic C#;
		- The previous bullet points can be combined to create very smart AIs that are capable of learning by integrating new data into their models of the game world.
