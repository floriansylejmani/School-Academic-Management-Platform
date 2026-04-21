using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace SchoolManagement.Application.Common.Validation;

public static class InputSanitizer
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "strong", "em", "u", "ol", "ul", "li"
    };

    private static readonly Dictionary<string, List<string>> AllowedAttributes = new()
    {
        ["a"] = new() { "href" },
        ["img"] = new() { "src", "alt" }
    };

    public static string SanitizeHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var doc = new HtmlDocument();
        doc.LoadHtml(input);

        SanitizeNode(doc.DocumentNode);

        return doc.DocumentNode.InnerHtml;
    }

    private static void SanitizeNode(HtmlNode node)
    {
        for (int i = node.ChildNodes.Count - 1; i >= 0; i--)
        {
            var child = node.ChildNodes[i];

            if (child.NodeType == HtmlNodeType.Element)
            {
                if (!AllowedTags.Contains(child.Name))
                {
                    // Remove disallowed tag but keep content
                    var parent = child.ParentNode;
                    while (child.HasChildNodes)
                    {
                        parent.InsertBefore(child.FirstChild, child);
                    }
                    parent.RemoveChild(child);
                }
                else
                {
                    // Remove disallowed attributes
                    var attributesToRemove = new List<HtmlAttribute>();
                    foreach (var attr in child.Attributes)
                    {
                        if (!AllowedAttributes.TryGetValue(child.Name, out var allowedAttrs) ||
                            !allowedAttrs.Contains(attr.Name))
                        {
                            attributesToRemove.Add(attr);
                        }
                    }

                    foreach (var attr in attributesToRemove)
                    {
                        child.Attributes.Remove(attr);
                    }

                    SanitizeNode(child);
                }
            }
        }
    }

    public static string SanitizeText(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove potentially dangerous characters
        var sanitized = Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty);

        // Normalize whitespace
        sanitized = Regex.Replace(sanitized, @"\s+", " ").Trim();

        // Limit length
        if (sanitized.Length > 1000)
        {
            sanitized = sanitized.Substring(0, 1000);
        }

        return sanitized;
    }

    public static string SanitizeEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return email; // Return as-is to let validation handle it

        email = email.ToLowerInvariant().Trim();
        
        // Remove potentially dangerous characters but preserve valid email format
        var sanitized = Regex.Replace(email, @"[\x00-\x1F\x7F]", string.Empty);
        
        return sanitized;
    }

    public static string SanitizePhoneNumber(string phone)
    {
        if (string.IsNullOrEmpty(phone))
            return string.Empty;

        // Remove potentially dangerous characters but preserve common phone formatting
        var sanitized = Regex.Replace(phone, @"[^\d\+\s\-\(\)]", string.Empty);
        
        // Limit to reasonable length
        if (sanitized.Length > 30)
        {
            sanitized = sanitized.Substring(0, 30);
        }

        // Trim whitespace
        return sanitized.Trim();
    }
}
