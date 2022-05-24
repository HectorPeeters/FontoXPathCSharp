using FontoXPathCSharp.Value;
using PrscSharp;
using static PrscSharp.PrscSharp;

namespace FontoXPathCSharp.Parsing;

public static class NameParser
{
    private static readonly ParseFunc<string> NcNameStartChar =
        Or(Regex(
                @"[A-Z_a-z\xC0-\xD6\xD8-\xF6\xF8-\u02FF\u0370-\u037D\u037F-\u1FFF\u200C\u200D\u2070-\u218F\u2C00-\u2FEF\u3001-\uD7FF\uF900-\uFDCF\uFDF0-\uFFFD]"),
            Then(Regex(@"[\uD800-\uDB7F]"), Regex(@"[\uDC00-\uDFFF]"), (a, b) => a + b));

    private static readonly ParseFunc<string> NcNameChar =
        Or(NcNameStartChar, Regex(@"[\-\.0-9\xB7\u0300-\u036F\u203F\u2040]"));

    private static readonly ParseFunc<string> NcName =
        Then(
            NcNameStartChar,
            Star(NcNameChar),
            (a, b) => a + string.Join("", b)
        );

    private static readonly ParseFunc<QName> UnprefixedName =
        Map(NcName, x => new QName(x, null, ""));

    private static readonly ParseFunc<QName> QName =
        Or(
            UnprefixedName
            // TODO: add prefixed name
        );

    // TODO: add uriQualifiedName
    public static readonly ParseFunc<QName> EqName = Or(QName);
}