using FontoXPathCSharp.Expressions;
using FontoXPathCSharp.Value;

namespace FontoXPathCSharp;

public static class CompileAstToExpression
{
    private static AbstractTestExpression CompileTestExpression(Ast ast)
    {
        return ast.Name switch
        {
            AstNodeName.NameTest => new NameTest(new QName(ast.TextContent, null, null)),
            _ => throw new InvalidDataException(ast.Name.ToString())
        };
    }

    private static AbstractExpression CompilePathExpression(Ast ast)
    {
        var steps = ast.GetChildren(AstNodeName.StepExpr).Select<Ast, AbstractExpression>(step =>
        {
            var axis = step.GetFirstChild(AstNodeName.XPathAxis);

            if (axis == null)
                throw new NotImplementedException();
            var test = step.GetFirstChild(new[]
            {
                AstNodeName.AttributeTest,
                AstNodeName.AnyElementTest,
                AstNodeName.PiTest,
                AstNodeName.DocumentTest,
                AstNodeName.ElementTest,
                AstNodeName.CommentTest,
                AstNodeName.NamespaceTest,
                AstNodeName.AnyKindTest,
                AstNodeName.TextTest,
                AstNodeName.AnyFunctionTest,
                AstNodeName.TypedFunctionTest,
                AstNodeName.SchemaAttributeTest,
                AstNodeName.AtomicType,
                AstNodeName.AnyItemType,
                AstNodeName.ParenthesizedItemType,
                AstNodeName.TypedMapTest,
                AstNodeName.TypedArrayTest,
                AstNodeName.NameTest,
                AstNodeName.Wildcard,
            });


            if (test == null)
                throw new InvalidOperationException("No test found in path expression axis");

            var testExpression = CompileTestExpression(test);

            return axis.TextContent switch
            {
                "self" => new SelfAxis(testExpression),
                "parent" => new ParentAxis(testExpression),
                _ => throw new NotImplementedException()
            };
        });

        return new PathExpression(steps.ToArray());
    }

    private static AbstractExpression CompileFunctionCallExpression(Ast ast)
    {
        var functionName = ast.GetFirstChild(AstNodeName.FunctionName);
        if (functionName == null)
            throw new InvalidDataException(ast.Name.ToString());

        var args = ast.GetFirstChild(AstNodeName.Arguments)?.GetChildren(AstNodeName.All);
        if (args == null)
            throw new InvalidDataException($"Missing args for {ast}");

        var argExpressions = args.Select(CompileAst).ToArray();

        return new FunctionCall(new NamedFunctionRef(functionName.GetQName(), args.Count()), argExpressions);
    }

    public static AbstractExpression CompileAst(Ast ast)
    {
        return ast.Name switch
        {
            AstNodeName.PathExpr => CompilePathExpression(ast),
            AstNodeName.FunctionCallExpr => CompileFunctionCallExpression(ast),
            _ => throw new InvalidDataException(ast.Name.ToString())
        };
    }
}