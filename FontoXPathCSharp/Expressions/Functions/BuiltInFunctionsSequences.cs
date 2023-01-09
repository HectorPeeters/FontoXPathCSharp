using FontoXPathCSharp.EvaluationUtils;
using FontoXPathCSharp.Sequences;
using FontoXPathCSharp.Value;
using FontoXPathCSharp.Value.Types;
using ValueType = FontoXPathCSharp.Value.Types.ValueType;

namespace FontoXPathCSharp.Expressions.Functions;

public static class BuiltInFunctionsSequences<TNode> where TNode : notnull
{
    private static readonly FunctionSignature<ISequence, TNode> FnExists = (_, _, _, args) =>
        SequenceFactory.CreateFromValue(new BooleanValue(!args[0].IsEmpty()));

    private static readonly FunctionSignature<ISequence, TNode> FnEmpty = (_, _, _, args) =>
        SequenceFactory.CreateFromValue(new BooleanValue(args[0].IsEmpty()));

    private static readonly FunctionSignature<ISequence, TNode> FnHead = (_, _, _, args) => 
        SubSequence(args[0], 1, 1);

    private static readonly FunctionSignature<ISequence, TNode> FnTail = (_, _, _, args) => 
        SubSequence(args[0], 2);

    private static readonly FunctionSignature<ISequence, TNode> FnInsertBefore = (_, _, _, args) =>
    {
        var sequence = args[0];
        var position = args[1];
        var inserts = args[2];
        
        if (sequence.IsEmpty()) {
            return inserts;
        }
    
        if (inserts.IsEmpty()) {
            return sequence;
        }
        var sequenceValue = sequence.GetAllValues();
    
        // XPath is 1 based
        var effectivePosition = position.First().GetAs<IntValue>().Value - 1;
        if (effectivePosition < 0) {
            effectivePosition = 0;
        } else if (effectivePosition > sequenceValue.Length) {
            effectivePosition = sequenceValue.Length;
        }

        var firstHalve = sequenceValue[..effectivePosition];
        var secondHalve = sequenceValue[effectivePosition..];
        
        return SequenceFactory.CreateFromArray(firstHalve.Concat(inserts.GetAllValues()).Concat(secondHalve).ToArray());
    };
    
    private static readonly FunctionSignature<ISequence, TNode> FnRemove = (_, _, _, args) =>
    {
        var sequence = args[0];
        var position = args[1];
        var effectivePosition = position.First()!.GetAs<IntValue>().Value;
        var sequenceValue = sequence.GetAllValues();
        if (
            sequenceValue.Length == 0 ||
            effectivePosition < 1 ||
            effectivePosition > sequenceValue.Length
        ) {
            return SequenceFactory.CreateFromArray(sequenceValue);
        }

        var sequenceValueList = sequenceValue.ToList();
        sequenceValueList.RemoveAt(effectivePosition - 1);
        return SequenceFactory.CreateFromArray(sequenceValueList.ToArray());
    };

    private static readonly FunctionSignature<ISequence, TNode> FnReverse = (_, _, _, args) =>
    {
        return args[0].MapAll(allValues =>
        {
            Array.Reverse(allValues);
            return SequenceFactory.CreateFromArray(allValues);
        });
    };
    
    private static readonly FunctionSignature<ISequence, TNode> FnCount = (_, _, _, args) =>
    {
        var hasPassed = false;
        return SequenceFactory.CreateFromIterator(_ =>
        {
            if (hasPassed) return IteratorResult<AbstractValue>.Done();

            hasPassed = true;
            return IteratorResult<AbstractValue>.Ready(new IntValue(args[0].GetLength()));
        }, 1);
    };

    private static readonly FunctionSignature<ISequence, TNode> FnMax = (_, _, _, args) =>
    {
        var sequence = args[0];
        if (sequence.IsEmpty()) return sequence;

        var items = CastItemsForMinMax(sequence.GetAllValues());

        // Use first element in array as initial value
        return SequenceFactory.CreateFromValue(
            items.Aggregate((max, item) =>
                Convert.ToDecimal(((AtomicValue)max).GetValue()) < Convert.ToDecimal(((AtomicValue)item).GetValue())
                    ? item
                    : max)
        );
    };

    private static readonly FunctionSignature<ISequence, TNode> FnMin = (_, _, _, args) =>
    {
        var sequence = args[0];
        if (sequence.IsEmpty()) return sequence;

        var items = CastItemsForMinMax(sequence.GetAllValues());

        // Use first element in array as initial value
        return SequenceFactory.CreateFromValue(
            items.Aggregate((max, item) =>
                Convert.ToDecimal(((AtomicValue)max).GetValue()) > Convert.ToDecimal(((AtomicValue)item).GetValue())
                    ? item
                    : max)
        );
    };

    private static readonly FunctionSignature<ISequence, TNode> FnAvg = (_, _, _, args) =>
    {
        var sequence = args[0];
        if (sequence.IsEmpty()) return sequence;


        // TODO: throw FORG0006 if the items contain both yearMonthDurations and dayTimeDurations
        var items = CastUntypedItemsToDouble(sequence.GetAllValues());
        items = CommonTypeUtils.ConvertItemsToCommonType(items)!;
        if (items == null) throw new XPathException("FORG0006", "Incompatible types to be converted to a common type");

        if (!items.All(item => item.GetValueType().IsSubtypeOf(ValueType.XsNumeric)))
            throw new XPathException("FORG0006", "Items passed to fn:avg are not all numeric.");

        var resultValue = items.Aggregate(0.0, (sum, item) =>
            sum + Convert.ToDouble(((AtomicValue)item).GetValue())) / items.Length;

        if (items.All(item => item.GetValueType().IsSubtypeOf(ValueType.XsInteger) ||
                              item.GetValueType().IsSubtypeOf(ValueType.XsDouble))
           )
            return SequenceFactory.CreateFromValue(AtomicValue.Create(resultValue, ValueType.XsDouble));

        if (items.All(item => { return item.GetValueType().IsSubtypeOf(ValueType.XsDecimal); })
           )
            return SequenceFactory.CreateFromValue(AtomicValue.Create(resultValue, ValueType.XsDecimal));

        return SequenceFactory.CreateFromValue(AtomicValue.Create(resultValue, ValueType.XsInteger));
    };


    private static readonly FunctionSignature<ISequence, TNode> FnZeroOrOne = (_, _, _, args) =>
    {
        var arg = args[0];
        if (!arg.IsEmpty() && !arg.IsSingleton())
            throw new XPathException("FORG0003", "The argument passed to fn:zero-or-one contained more than one item.");

        return arg;
    };

    private static readonly FunctionSignature<ISequence, TNode> FnOneOrMore = (_, _, _, args) =>
    {
        var arg = args[0];
        if (arg.IsEmpty()) throw new XPathException("FORG0004", "The argument passed to fn:one-or-more was empty.");
        return arg;
    };

    private static readonly FunctionSignature<ISequence, TNode> FnExactlyOne = (_, _, _, args) =>
    {
        var arg = args[0];
        if (!arg.IsSingleton())
            throw new XPathException("FORG0005",
                "The argument passed to fn:exactly-one is empty or contained more than one item.");
        return arg;
    };

    private static readonly FunctionSignature<ISequence, TNode> FnDeepEqual =
        (dynamicContext, executionParameters, staticContext, args) =>
        {
            var hasPassed = false;
            var deepEqualityIterator = BuiltInFunctionsSequencesDeepEqual<TNode>.SequenceDeepEqual(
                dynamicContext!,
                executionParameters,
                staticContext!,
                args[0],
                args[1]
            );

            return SequenceFactory.CreateFromIterator(
                _ =>
                {
                    if (hasPassed) return IteratorResult<BooleanValue>.Done();

                    var result = deepEqualityIterator(IterationHint.None);
                    if (result.IsDone) return result;

                    hasPassed = true;
                    return IteratorResult<BooleanValue>.Ready(result.Value!);
                }
            );
        };


    public static readonly BuiltinDeclarationType<TNode>[] Declarations =
    {
        new(new[]
            {
                new ParameterType(ValueType.Item, SequenceMultiplicity.ZeroOrMore)
            },
            FnEmpty,
            "empty",
            BuiltInUri.FunctionsNamespaceUri.GetBuiltinNamespaceUri(),
            new SequenceType(ValueType.XsBoolean, SequenceMultiplicity.ExactlyOne)
        ),

        new(new[]
            {
                new ParameterType(ValueType.Item, SequenceMultiplicity.ZeroOrMore)
            },
            FnExists,
            "exists",
            BuiltInUri.FunctionsNamespaceUri.GetBuiltinNamespaceUri(),
            new SequenceType(ValueType.XsBoolean, SequenceMultiplicity.ExactlyOne)
        ),

        new(new[]
            {
                new ParameterType(ValueType.Item, SequenceMultiplicity.ZeroOrMore)
            },
            FnHead,
            "head",
            BuiltInUri.FunctionsNamespaceUri.GetBuiltinNamespaceUri(),
            new SequenceType(ValueType.Item, SequenceMultiplicity.ZeroOrOne)
        ),

        new(new[]
            {
                new ParameterType(ValueType.Item, SequenceMultiplicity.ZeroOrMore)
            },
            FnTail,
            "tail",
            BuiltInUri.FunctionsNamespaceUri.GetBuiltinNamespaceUri(),
            new SequenceType(ValueType.Item, SequenceMultiplicity.ZeroOrMore)
        ),
        
        new(new[]
            {
                new ParameterType(ValueType.Item, SequenceMultiplicity.ZeroOrMore),
                new ParameterType(ValueType.XsInteger, SequenceMultiplicity.ExactlyOne),
                new ParameterType(ValueType.Item, SequenceMultiplicity.ZeroOrMore),
            },
            FnInsertBefore,
            "insert-before",
            BuiltInUri.FunctionsNamespaceUri.GetBuiltinNamespaceUri(),
            new SequenceType(ValueType.Item, SequenceMultiplicity.ZeroOrMore)
        ),

        new(new[]
            {
                new ParameterType(ValueType.Item, SequenceMultiplicity.ZeroOrMore),
                new ParameterType(ValueType.XsInteger, SequenceMultiplicity.ExactlyOne),
            },
            FnRemove,
            "remove",
            BuiltInUri.FunctionsNamespaceUri.GetBuiltinNamespaceUri(),
            new SequenceType(ValueType.Item, SequenceMultiplicity.ZeroOrMore)
        ),
        
        new(new[]
            {
                new ParameterType(ValueType.Item, SequenceMultiplicity.ZeroOrMore),
            },
            FnReverse,
            "reverse",
            BuiltInUri.FunctionsNamespaceUri.GetBuiltinNamespaceUri(),
            new SequenceType(ValueType.Item, SequenceMultiplicity.ZeroOrMore)
        ),
        
        new(new[]
            {
                new ParameterType(ValueType.Item, SequenceMultiplicity.ZeroOrOne)
            },
            FnCount,
            "count",
            BuiltInUri.FunctionsNamespaceUri.GetBuiltinNamespaceUri(),
            new SequenceType(ValueType.XsInteger, SequenceMultiplicity.ExactlyOne)
        ),

        new(new[]
            {
                new ParameterType(ValueType.XsAnyAtomicType, SequenceMultiplicity.ZeroOrMore)
            },
            FnAvg,
            "avg",
            BuiltInUri.FunctionsNamespaceUri.GetBuiltinNamespaceUri(),
            new SequenceType(ValueType.XsAnyAtomicType, SequenceMultiplicity.ZeroOrOne)
        ),

        new(new[]
            {
                new ParameterType(ValueType.XsAnyAtomicType, SequenceMultiplicity.ZeroOrMore)
            },
            FnMax,
            "max",
            BuiltInUri.FunctionsNamespaceUri.GetBuiltinNamespaceUri(),
            new SequenceType(ValueType.XsAnyAtomicType, SequenceMultiplicity.ZeroOrOne)
        ),

        new(new[]
            {
                new ParameterType(ValueType.XsAnyAtomicType, SequenceMultiplicity.ZeroOrMore),
                new ParameterType(ValueType.XsString, SequenceMultiplicity.ExactlyOne)
            },
            (_, _, _, _) => throw new XPathException("FOCH0002", "No collations are supported"),
            "max",
            BuiltInUri.FunctionsNamespaceUri.GetBuiltinNamespaceUri(),
            new SequenceType(ValueType.XsAnyAtomicType, SequenceMultiplicity.ZeroOrOne)
        ),

        new(new[]
            {
                new ParameterType(ValueType.XsAnyAtomicType, SequenceMultiplicity.ZeroOrMore)
            },
            FnMin,
            "min",
            BuiltInUri.FunctionsNamespaceUri.GetBuiltinNamespaceUri(),
            new SequenceType(ValueType.XsAnyAtomicType, SequenceMultiplicity.ZeroOrOne)
        ),

        new(new[]
            {
                new ParameterType(ValueType.XsAnyAtomicType, SequenceMultiplicity.ZeroOrMore),
                new ParameterType(ValueType.XsString, SequenceMultiplicity.ExactlyOne)
            },
            (_, _, _, _) => throw new XPathException("FOCH0002", "No collations are supported"),
            "min",
            BuiltInUri.FunctionsNamespaceUri.GetBuiltinNamespaceUri(),
            new SequenceType(ValueType.XsAnyAtomicType, SequenceMultiplicity.ZeroOrOne)
        ),

        new(new[]
            {
                new ParameterType(ValueType.Item, SequenceMultiplicity.ZeroOrMore)
            },
            FnZeroOrOne,
            "zero-or-one",
            BuiltInUri.FunctionsNamespaceUri.GetBuiltinNamespaceUri(),
            new SequenceType(ValueType.Item, SequenceMultiplicity.ZeroOrOne)
        ),

        new(new[]
            {
                new ParameterType(ValueType.Item, SequenceMultiplicity.ZeroOrMore)
            },
            FnOneOrMore,
            "one-or-more",
            BuiltInUri.FunctionsNamespaceUri.GetBuiltinNamespaceUri(),
            new SequenceType(ValueType.Item, SequenceMultiplicity.ZeroOrOne)
        ),

        new(new[]
            {
                new ParameterType(ValueType.Item, SequenceMultiplicity.ZeroOrMore)
            },
            FnExactlyOne,
            "exactly-one",
            BuiltInUri.FunctionsNamespaceUri.GetBuiltinNamespaceUri(),
            new SequenceType(ValueType.Item, SequenceMultiplicity.ZeroOrOne)
        ),

        new(new[]
            {
                new ParameterType(ValueType.Item, SequenceMultiplicity.ZeroOrMore),
                new ParameterType(ValueType.Item, SequenceMultiplicity.ZeroOrMore)
            },
            FnDeepEqual,
            "deep-equal",
            BuiltInUri.FunctionsNamespaceUri.GetBuiltinNamespaceUri(),
            new SequenceType(ValueType.XsBoolean, SequenceMultiplicity.ExactlyOne)
        ),

        new(new[]
            {
                new ParameterType(ValueType.Item, SequenceMultiplicity.ZeroOrMore),
                new ParameterType(ValueType.Item, SequenceMultiplicity.ZeroOrMore),
                new ParameterType(ValueType.XsString, SequenceMultiplicity.ExactlyOne)
            },
            (_, _, _, _) => throw new XPathException("FOCH0002", "No collations are supported"),
            "deep-equal",
            BuiltInUri.FunctionsNamespaceUri.GetBuiltinNamespaceUri(),
            new SequenceType(ValueType.XsBoolean, SequenceMultiplicity.ExactlyOne)
        )
    };

    private static ISequence SubSequence(ISequence sequence, int start, int? length = null)
    {
        // XPath starts from 1
        var i = 1;
        var iterator = sequence.GetValue();

        var predictedLength = sequence.GetLength();
        int? newSequenceLength = null;
        var startIndex = Math.Max(start - 1, 0);
        if (predictedLength != -1)
        {
            var endIndex = length == null
                ? predictedLength
                : Math.Max(0, Math.Min(predictedLength, (int)length + (start - 1)));
            newSequenceLength = Math.Max(0, endIndex - startIndex);
        }

        return SequenceFactory.CreateFromIterator(
            hint =>
            {
                while (i < start)
                {
                    iterator(hint);
                    i++;
                }

                if (length != null && i >= start + length) return IteratorResult<AbstractValue>.Done();

                var returnableVal = iterator(hint);
                i++;

                return returnableVal;
            },
            newSequenceLength
        );
    }

    private static AbstractValue[] CastItemsForMinMax(AbstractValue[] items)
    {
        // Values of type xs:untypedAtomic in $arg are cast to xs:double.
        items = CastUntypedItemsToDouble(items);

        if (items.Any(item => double.IsNaN(item.GetAs<DoubleValue>().Value) ||
                              float.IsNaN(item.GetAs<FloatValue>().Value)))
            return new AbstractValue[] { AtomicValue.Create(double.NaN, ValueType.XsDouble) };

        var convertResult = CommonTypeUtils.ConvertItemsToCommonType(items);

        if (convertResult == null)
            throw new XPathException("FORG0006", "Incompatible types to be converted to a common type");

        return convertResult!;
    }

    private static AbstractValue[] CastUntypedItemsToDouble(AbstractValue[] items)
    {
        return items.Select(item =>
            item.GetValueType().IsSubtypeOf(ValueType.XsUntypedAtomic)
                ? item.CastToType(ValueType.XsDouble)
                : item).ToArray();
    }
}