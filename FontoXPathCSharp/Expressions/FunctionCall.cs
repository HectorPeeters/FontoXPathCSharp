using FontoXPathCSharp.Sequences;
using FontoXPathCSharp.Value;
using ValueType = FontoXPathCSharp.Value.ValueType;

namespace FontoXPathCSharp.Expressions;

public class FunctionCall : PossiblyUpdatingExpression
{
    private readonly int _callArity;
    private FunctionValue<ISequence>? _functionReference;
    private readonly AbstractExpression _functionReferenceExpression;
    private StaticContext? _staticContext;

    public FunctionCall(AbstractExpression functionReferenceExpression) : base(
        new[] {functionReferenceExpression} /* TODO: add arguments here */,
        new OptimizationOptions(false))
    {
        _callArity = 0;
        _functionReferenceExpression = functionReferenceExpression;
        _staticContext = null;
    }

    public override ISequence PerformFunctionalEvaluation(DynamicContext? dynamicContext,
        ExecutionParameters? executionParameters)
    {
        if (_functionReference != null)
        {
            return _functionReference.Value(dynamicContext, executionParameters, null, Array.Empty<ISequence>());
        }

        // TODO: perform other evaluation
        throw new NotImplementedException();
    }

    public override void PerformStaticEvaluation(StaticContext staticContext)
    {
        _staticContext = staticContext.Clone();

        base.PerformStaticEvaluation(staticContext);

        if (_functionReferenceExpression.CanBeStaticallyEvaluated)
        {
            var functionRefSequence = _functionReferenceExpression.EvaluateMaybeStatically(null, null);
            if (!functionRefSequence.IsSingleton())
            {
                throw new XPathException("XPTY0004");
            }

            _functionReference = ValidateFunctionItem<ISequence>(functionRefSequence.First()!, _callArity);

            // TODO: check if function reference is updating
        }
    }

    private static FunctionValue<T> ValidateFunctionItem<T>(AbstractValue item, int callArity)
    {
        var functionItem = item.GetAs<FunctionValue<T>>(ValueType.Function);

        if (functionItem == null)
        {
            throw new XPathException("Expected base expression to evaluate to a function item");
        }

        if (functionItem.GetArity() != callArity)
        {
            throw new XPathException("XPTY0004");
        }

        return functionItem;
    }
}