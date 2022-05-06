using FontoXPathCSharp.Sequences;
using FontoXPathCSharp.Value;
using FontoXPathCSharp.Value.Types;
using ValueType = FontoXPathCSharp.Value.Types.ValueType;

namespace FontoXPathCSharp.Expressions.Functions;

public static class BuiltInFunctionsString
{
    private static readonly FunctionSignature<ISequence> FnStringLength = (context, parameters, staticContext, args) =>
    {
        if (args.Length == 0) return new SingletonSequence(new IntValue(0));

        var stringValue = args[0].First()!.GetAs<StringValue>(ValueType.XsString)!.Value;

        return new SingletonSequence(new IntValue(stringValue.Length));
    };

    public static readonly BuiltinDeclarationType[] Declarations =
    {
        new(new[] {new ParameterType(ValueType.Node, SequenceMultiplicity.ZeroOrOne)},
            FnStringLength, "string-length",
            "http://www.w3.org/2005/xpath-functions",
            new SequenceType(ValueType.XsInteger, SequenceMultiplicity.ExactlyOne)),

        new(Array.Empty<ParameterType>(),
            BuiltInFunctions.ContextItemAsFirstArgument(FnStringLength), "string-length",
            "http://www.w3.org/2005/xpath-functions",
            new SequenceType(ValueType.XsInteger, SequenceMultiplicity.ExactlyOne))
    };
}