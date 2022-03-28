﻿using System.Xml;
using FontoXPathCSharp;
using FontoXPathCSharp.Parsing;
using FontoXPathCSharp.Sequences;
using FontoXPathCSharp.Value;
using FontoXPathCSharp.Value.Types;
using ValueType = FontoXPathCSharp.Value.Types.ValueType;

const string query = "test(self::p)";
const string xml = "<p>Test</p>";

Console.WriteLine($"Running: `{query}`\n");

var parser = XPathParser.FunctionCall();
var result = parser(query, 0).UnwrapOr((expected, fatal) =>
{
    Console.WriteLine("Parsing error ({0}): {1}", fatal, string.Join(", ", expected));
    Environment.Exit(1);
    return new Ast("ERROR");
});
Console.WriteLine("Parsed query: ");
Console.WriteLine(result);

var xmlDocument = new XmlDocument();
xmlDocument.LoadXml(xml);
var document = xmlDocument.FirstChild!;

Console.WriteLine("\nResult:");
var expr = CompileAstToExpression.CompileAst(result);
var staticContext = new StaticContext();

staticContext.RegisterFunctionDefinition(new FunctionProperties(
    new[] {new ParameterType(ValueType.Node, SequenceMultiplicity.ExactlyOne)}, 1,
    (context, parameters, staticContext, args) =>
    {
        Console.WriteLine("Called test function");
        return args[0];
    }, "test", "", new SequenceType(ValueType.Node, SequenceMultiplicity.ExactlyOne)));

expr.PerformStaticEvaluation(staticContext);
Console.WriteLine(expr.Evaluate(new DynamicContext(new NodeValue(document), 0), new ExecutionParameters(document)));