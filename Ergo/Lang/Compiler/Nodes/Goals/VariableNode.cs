namespace Ergo.Lang.Compiler;

public class VariableNode(Variable v) : ExecutionNode
{
    public Variable Binding { get; private set; } = v;

    public override ErgoVM.Op Compile() => vm => ErgoVM.Ops.Goal(vm.Memory.StoreVariable(Binding.Name));
    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new VariableNode((Variable)Binding.Instantiate(ctx, vars));
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        var term = ((ITerm)Binding).Substitute(s);
        if (term is not Variable)
            return new DynamicNode(term);
        return new VariableNode((Variable)term);
    }
    public override string Explain(bool canonical = false) => Binding.Name;
}
