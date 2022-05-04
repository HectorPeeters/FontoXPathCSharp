namespace FontoXPathCSharp.Value;

public class QName
{
    public readonly string LocalName;
    public readonly string? NamespaceUri;
    public readonly string Prefix;

    public QName(string localName, string? namespaceUri, string? prefix)
    {
        LocalName = localName;
        NamespaceUri = namespaceUri;
        Prefix = prefix ?? "";
    }

    public Ast GetAst(AstNodeName name)
    {
        var ast = new Ast(name)
        {
            TextContent = LocalName,
            StringAttributes =
            {
                ["URI"] = Prefix
            }
        };

        if (NamespaceUri != null)
            ast.StringAttributes["URI"] = NamespaceUri;

        return ast;
    }

    public override string ToString()
    {
        return $"Q{{{NamespaceUri ?? ""}}}{(Prefix == "" ? "" : Prefix + ":") + LocalName}";
    }
}