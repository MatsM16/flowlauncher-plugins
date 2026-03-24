using System;
using System.Collections.Generic;
using System.Linq;
using Flow.Launcher.Plugin;

namespace Uuid.Plugin;

/// <summary>
/// FlowLauncher plugin for UUIDs. This plugin allows users to generate or format UUIDs directly from FlowLauncher.
/// </summary>
public class UuidPlugin : IPlugin
{
    private PluginInitContext _context;

    /// <inheritdoc />
    public void Init(PluginInitContext context) => _context = context;

    /// <inheritdoc />
    public List<Result> Query(Query query)
    {
        if (query.SearchTerms.Length is 2)
        {
            if (Guid.TryParse(query.SearchTerms[0], out var parsed0))
                return CreateVariants(parsed0, query.SearchTerms[1], query.SearchTerms[0]);
            if (Guid.TryParse(query.SearchTerms[1], out var parsed1))
                return CreateVariants(parsed1, query.SearchTerms[0], query.SearchTerms[1]);
        }
        else if (query.SearchTerms.Length is 1)
        {
            if (Guid.TryParse(query.SearchTerms[0], out var parsed0))
                return CreateVariants(parsed0, "", query.SearchTerms[0]);
        }
        
        return GenerateNew(query.Search);
    }

    private List<Result> GenerateNew(string flags)
    {
        var generated = flags.Contains('0') ? Guid.Empty
            : flags.Contains('7') ? Guid.CreateVersion7() 
            : Guid.NewGuid();

        return CreateVariants(generated, flags);
    }

    private List<Result> CreateVariants(Guid guid, string flags, string original = null)
    {
        var formats = ContainsOneOf(flags, 'n', 'N') ? "N" 
            : ContainsOneOf(flags, 'd', 'D') ? "D" 
            : "DN";

        var quotes = flags.Contains('"') ? "\"\""
            : flags.Contains('\'') ? "''"
            : ContainsOneOf(flags, '(', ')') ? "()"
            : ContainsOneOf(flags, '<', '>') ? "<>"
            : ContainsOneOf(flags, '[', ']') ? "[]"
            : ContainsOneOf(flags, '{', '}') ? "{}"
            : null;

        return formats
            .Select(c => guid.ToString(c.ToString()))
            .Select(uuid => quotes != null ? $"{quotes[0]}{uuid}{quotes[1]}" : uuid)
            .Where(uuid => !string.Equals(uuid, original, StringComparison.OrdinalIgnoreCase))
            .Select(uuid => new Result
            {
                Title = uuid,
                SubTitle = "Copy to clipboard",
                IcoPath = "Images\\uuid.png",
                CopyText = uuid,
                Action = _ =>
                {
                    _context.API.CopyToClipboard(uuid);
                    _context.API.ReQuery();
                    return true;
                }
            })
            .ToList();
    }

    private static bool ContainsOneOf(string input, char c1, char c2) => input.Contains(c1) || input.Contains(c2);
}
