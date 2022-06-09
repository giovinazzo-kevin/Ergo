
global using Ergo.Lang;
global using Ergo.Lang.Ast;
global using Ergo.Lang.Exceptions;
global using Ergo.Interpreter;
global using Ergo.Interpreter.Directives;
global using Ergo.Shell;
global using Ergo.Lang.Extensions;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;
using WikiClientLibrary.Client;
using WikiClientLibrary.Wikibase;
using System.Text.RegularExpressions;
using Ergo.Solver.BuiltIns;
using Ergo.Solver;
using static Ergo.Lang.Ast.WellKnown;
using Newtonsoft.Json.Linq;
using System.Web;
using Newtonsoft.Json;

//var ret = await new WikiDataSparqlClient().Query(@"
//SELECT ?game ?gameLabel (MIN(?date) AS ?releaseDate)
//WHERE
//{
//  ?game   wdt:P31 wd:Q7889;      # instance of: video game
//          wdt:P404 wd:Q1758804;  # game mode: co-op mode
//          wdt:P136 wd:Q185029;   # genre: first-person shooter
//          wdt:P577 ?date.        # publication date
//  SERVICE wikibase:label { bd:serviceParam wikibase:language ""[AUTO_LANGUAGE], en"". }
//}
//GROUP BY ?gameLabel ?game
//ORDER BY DESC(?releaseDate)
//LIMIT 10
//");

var shell = new ErgoShell(configureSolver: solver =>
{
    solver.TryAddBuiltIn(new EntityBuiltIn());
    solver.TryAddBuiltIn(new PropertyBuiltIn());
    solver.TryAddBuiltIn(new EntityClaimBuiltIn());
    solver.TryAddBuiltIn(new ClaimPropertyValueBuiltIn());
    solver.TryAddBuiltIn(new ClaimQualifierBuiltIn());
});
var module = new Module(new("wikidata"), runtime: true);
var scope = shell.CreateScope();
//scope = scope.WithInterpreterScope(scope.InterpreterScope
//    .WithModule(module)
//    .WithCurrentModule(module.Name)
//    .WithRuntime(true));
shell.Load(ref scope, "wd_tests");
await foreach(var _ in shell.Repl(scope));

public static class SnakExtensions
{
    public static void FixInvalidValues(this Snak s)
    {
        if (s.DataType.Name == "time")
        {
            var fmt = s.RawDataValue is not null
                ? s.RawDataValue["value"]["time"].ToString()
                : string.Empty;
            s.DataValue = fmt ?? string.Empty;
        }
        if (s.DataValue is null)
        {
            s.DataValue = string.Empty;
        }
    }
}

public class ClaimPropertyValueBuiltIn : BuiltIn
{
    public ClaimPropertyValueBuiltIn()
        : base("", new("claim_property_value"), Maybe.Some(3), new("wikidata"))
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        if (args[0].Is<Claim>(out var claim))
        {
            args[0] = new Atom(claim.MainSnak);
        }
        if (args[0].Is<Snak>(out var snak))
        {
            var pKey = snak.PropertyId;
            var pVal = snak.DataValue;
            if (args[1].Matches<string>(out var key))
            {
                if (pKey?.Equals(key) ?? false)
                {
                    if (args[2] is Atom a && (a.Value?.Equals(pVal) ?? false))
                    {
                        yield return new Evaluation(Literals.True);
                    }
                    else if (!args[2].IsGround)
                    {
                        yield return new Evaluation(Literals.True, new Substitution(args[2], new Atom(pVal)));
                    }
                    else
                    {
                        yield return new Evaluation(Literals.False);
                    }
                }
                else
                {
                    yield return new Evaluation(Literals.False);
                }
            }
            else if (!args[1].IsGround)
            {
                if (args[2] is Atom a && (a.Value?.Equals(claim.MainSnak.DataValue) ?? false))
                {
                    yield return new Evaluation(Literals.True, new Substitution(args[1], new Atom(pKey)));
                }
                else if (!args[2].IsGround)
                {
                    yield return new Evaluation(Literals.True, new Substitution(args[1], new Atom(pKey)), new Substitution(args[2], new Atom(pVal)));
                }
                else
                {
                    yield return new Evaluation(Literals.False);
                }
            }
            else
            {
                yield return new Evaluation(Literals.False);
            }
        }
        else if (!args[0].IsGround)
        {
            throw new SolverException(SolverError.TermNotSufficientlyInstantiated, scope, args[0].Explain());
        }
        else
        {
            yield return new Evaluation(Literals.False);
        }
    }
}

public class ClaimQualifierBuiltIn : BuiltIn
{
    public ClaimQualifierBuiltIn()
        : base("", new("claim_qualifier"), Maybe.Some(2), new("wikidata"))
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        if (args[0].Is<Claim>(out var claim))
        {
            if (!args[1].IsGround)
            {
                if(claim.Qualifiers.Count == 0)
                {
                    yield return new Evaluation(Literals.False);
                }
                else
                {
                    foreach (var qualifier in claim.Qualifiers)
                    {
                        qualifier.FixInvalidValues();
                        yield return new Evaluation(Literals.True, new Substitution(args[1], new Atom(qualifier)));
                    }
                }
            }
            else if (args[1].Is<Snak>(out var qualifier))
            {
                if (claim.Qualifiers.Contains(qualifier))
                {
                    yield return new Evaluation(Literals.True);
                }
                else
                {
                    yield return new Evaluation(Literals.False);
                }
            }
            else
            {
                yield return new Evaluation(Literals.False);
            }
        }
        else if (!args[0].IsGround)
        {
            throw new SolverException(SolverError.TermNotSufficientlyInstantiated, scope, args[0].Explain());
        }
        else
        {
            yield return new Evaluation(Literals.False);
        }
    }
}

public class EntityClaimBuiltIn : BuiltIn
{
    public EntityClaimBuiltIn()
        : base("", new("entity_claim"), Maybe.Some(2), new("wikidata"))
    {
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        if(args[0].Is<Entity>(out var entity))
        {
            if(!args[1].IsGround)
            {
                if(entity.Claims.Count == 0)
                {
                    yield return new Evaluation(Literals.False);
                }
                else
                {
                    foreach (var claim in entity.Claims)
                    {
                        claim.MainSnak.FixInvalidValues();
                        yield return new Evaluation(Literals.True, new Substitution(args[1], new Atom(claim)));
                    }
                }
            }
            else if(args[1].Is<Claim>(out var claim))
            {
                if(entity.Claims.Contains(claim))
                {
                    yield return new Evaluation(Literals.True);
                }
                else
                {
                    yield return new Evaluation(Literals.False);
                }
            }
            else
            {
                yield return new Evaluation(Literals.False);
            }
        }
        else if(!args[0].IsGround)
        {
            throw new SolverException(SolverError.TermNotSufficientlyInstantiated, scope, args[0].Explain());
        }
        else
        {
            yield return new Evaluation(Literals.False);
        }
    }
}

public class EntityBuiltIn : BuiltIn
{
    protected static readonly Dictionary<int, Entity> Cache = new();
    const string URL = "https://www.wikidata.org/w/api.php";
    public EntityBuiltIn()
        : base("", new("entity"), Maybe.Some(2), new("wikidata"))
    {
    }

    public static async Task<Entity> FetchEntity(int code)
    {
        if(Cache.TryGetValue(code, out var entity))
        {
            return entity;
        }
        using var client = new WikiClient();
        var site = new WikiSite(client, URL);
        await site.Initialization;
        entity = Cache[code] = new Entity(site, $"Q{code}");
        await entity.RefreshAsync(
            EntityQueryOptions.FetchClaims | EntityQueryOptions.FetchLabels, 
            new[] { "en" }
        );
        return entity;
    }

    private async IAsyncEnumerable<Evaluation> ApplyInternal(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        if (args[0].Matches<string>(out var wdString) && wdString.StartsWith('Q') && int.TryParse(wdString[1..], out var wd))
        {
            if (!args[1].IsGround)
            {
                var entity = FetchEntity(wd).GetAwaiter().GetResult();
                yield return new Evaluation(Literals.True, new Substitution(args[1], new Atom(entity)));
            }
            else if (args[1].Is<Entity>(out var entity))
            {
                if (entity.PageId == wd)
                {
                    yield return new Evaluation(Literals.True);
                }
                else
                {
                    yield return new Evaluation(Literals.False);
                }
            }
            else
            {
                yield return new Evaluation(Literals.False);
            }
        }
        else if (!args[0].IsGround)
        {
            if (!args[1].IsGround)
            {
                foreach (var (ans, i) in Enumerable.Range(1, int.MaxValue)
                    .SelectMany(i => ApplyInternal(solver, scope, new[] { new Atom($"Q{i}"), args[1] }).CollectAsync().GetAwaiter().GetResult())
                        .Select((ans, i) => (ans, i)))
                {
                    yield return new Evaluation(Literals.True, ans.Substitutions.Append(new Substitution(args[0], new Atom($"Q{i + 1}"))).ToArray());
                }
            }
            else if (args[1].Is<Entity>(out var entity))
            {
                yield return new Evaluation(Literals.True, new Substitution(args[0], new Atom($"Q{entity.PageId}")));
            }
            else
            {
                yield return new Evaluation(Literals.False);
            }
        }
        else
        {
            yield return new Evaluation(Literals.False);
        }
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        var any = false;
        await foreach (var ans in ApplyInternal(solver, scope, args))
        {
            // Skip answers where the entity doesn't exist
            if (!ans.Substitutions.Any(sub => sub.Rhs.Is<Entity>(out var entity, e => !e.Exists)))
            {
                yield return ans;
                any = true;
            }
        }
        if(!any)
        {
            yield return new Evaluation(Literals.False);
        }
    }
}

public sealed class WikiDataSparqlClient
{
    const string URL = "https://query.wikidata.org/sparql?query=";

    public async Task<JObject> Query(string sparql)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; Fieropa/1.0)");
        client.DefaultRequestHeaders.Add("Accept", "application/sparql-results+json");
        var ret = await client.GetAsync(URL + HttpUtility.UrlEncode(sparql));
        ret.EnsureSuccessStatusCode();
        var json = await ret.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<JObject>(json);
    }
}


public class PropertyBuiltIn : BuiltIn
{
    protected static readonly Dictionary<int, Entity> Cache = new();
    const string URL = "https://www.wikidata.org/w/api.php";
    public PropertyBuiltIn()
        : base("", new("property"), Maybe.Some(2), new("wikidata"))
    {
    }

    public static async Task<Entity> FetchEntity(int code)
    {
        if (Cache.TryGetValue(code, out var entity))
        {
            return entity;
        }
        using var client = new WikiClient();
        var site = new WikiSite(client, URL);
        await site.Initialization;
        entity = Cache[code] = new Entity(site, $"P{code}");
        await entity.RefreshAsync(
            EntityQueryOptions.FetchClaims | EntityQueryOptions.FetchLabels,
            new[] { "en" }
        );
        return entity;
    }

    private async IAsyncEnumerable<Evaluation> ApplyInternal(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        if (args[0].Matches<string>(out var wdString) && wdString.StartsWith('P') && int.TryParse(wdString[1..], out var wd))
        {
            if (!args[1].IsGround)
            {
                var entity = FetchEntity(wd).GetAwaiter().GetResult();
                yield return new Evaluation(Literals.True, new Substitution(args[1], new Atom(entity)));
            }
            else if (args[1].Is<Entity>(out var entity))
            {
                if (entity.PageId == wd)
                {
                    yield return new Evaluation(Literals.True);
                }
                else
                {
                    yield return new Evaluation(Literals.False);
                }
            }
            else
            {
                yield return new Evaluation(Literals.False);
            }
        }
        else if (!args[0].IsGround)
        {
            if (!args[1].IsGround)
            {
                foreach (var (ans, i) in Enumerable.Range(1, int.MaxValue)
                    .SelectMany(i => ApplyInternal(solver, scope, new[] { new Atom($"Q{i}"), args[1] }).CollectAsync().GetAwaiter().GetResult())
                        .Select((ans, i) => (ans, i)))
                {
                    yield return new Evaluation(Literals.True, ans.Substitutions.Append(new Substitution(args[0], new Atom($"P{i + 1}"))).ToArray());
                }
            }
            else if (args[1].Is<Entity>(out var entity))
            {
                yield return new Evaluation(Literals.True, new Substitution(args[0], new Atom($"P{entity.PageId}")));
            }
            else
            {
                yield return new Evaluation(Literals.False);
            }
        }
        else
        {
            yield return new Evaluation(Literals.False);
        }
    }

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        var any = false;
        await foreach (var ans in ApplyInternal(solver, scope, args))
        {
            // Skip answers where the entity doesn't exist
            if (!ans.Substitutions.Any(sub => sub.Rhs.Is<Entity>(out var entity, e => !e.Exists)))
            {
                yield return ans;
                any = true;
            }
        }
        if (!any)
        {
            yield return new Evaluation(Literals.False);
        }
    }
}