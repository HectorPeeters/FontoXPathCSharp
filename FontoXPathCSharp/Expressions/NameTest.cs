using FontoXPathCSharp.Value;
using ValueType = FontoXPathCSharp.Value.ValueType;

namespace FontoXPathCSharp.Expressions;

public class NameTest : AbstractTestExpression
{
    private readonly QName _name;

    public NameTest(QName name)
    {
        _name = name;
    }

    protected internal override bool EvaluateToBoolean(
        DynamicContext? _,
        AbstractValue value,
        ExecutionParameters? executionParameters)
    {
        var node = value.GetAs<NodeValue>(ValueType.Node)!.Value();   
        
        if (_name.Prefix == null && _name.NamespaceUri != "" && _name.LocalName == "*")
        {
            return true;
        }

        if (_name.Prefix == "*")
        {
            if (_name.LocalName == "*")
            {
                return true;
            }

            return _name.LocalName == node.LocalName;
        }

        if (_name.LocalName != "*")
        {
            if (_name.LocalName != node.LocalName)
            {
                return false;
            }
        }

        // TODO: there is a lot more to add here
        return true;
    }
}