using Markdig.Syntax;
using Symptum.Core.Management.Resources;
using Symptum.Markdown;
using Symptum.Markdown.Embedding;
using Symptum.UI.Markdown.Renderers;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown;

public class ImportsHandler
{
    private Dictionary<string, ImportBlockElement> importBlocks = [];

    private const int MaxCacheSize = 50;
    private static readonly Dictionary<string, (MarkdownDocument Doc, LinkedListNode<string> Node)> _parseCache = new(StringComparer.Ordinal);
    private static readonly LinkedList<string> _lruOrder = new();
    private static readonly object _parseCacheLock = new();

    public void RegisterForImport(string importId, ImportBlockElement importBlockElement)
    {
        importBlocks.TryAdd(importId, importBlockElement);
    }

    internal void ResolveImports(IEnumerable<ExportBlock>? availableExports, WinUIRenderer renderer)
    {
        foreach (var kvp in importBlocks)
        {
            string id = kvp.Key;
            ExportBlock? match = null;
            if (id.StartsWith(nameof(Symptum)))
            {
                var ids = id.Split('?');
                if (ids.Length != 2) return;
                string resId = ids[0];
                string impId = ids[1];

                if (ResourceManager.TryGetResourceById(resId, out IResource? resource)
                    && resource is MarkdownFileResource markdownFileResource
                    && !string.IsNullOrEmpty(markdownFileResource.Markdown))
                {
                    // Pre-process reference inlines using the source document's dependencies.
                    string sourceMd = MarkdownManager.OptimizeReferences(markdownFileResource.Markdown, markdownFileResource) ?? string.Empty;

                    MarkdownDocument doc;
                    lock (_parseCacheLock)
                    {
                        if (_parseCache.TryGetValue(sourceMd, out var entry))
                        {
                            _lruOrder.Remove(entry.Node);
                            _lruOrder.AddFirst(entry.Node);
                            doc = entry.Doc;
                        }
                        else
                        {
                            doc = Markdig.Markdown.Parse(sourceMd, MarkdownManager.Pipeline);
                            var node = _lruOrder.AddFirst(sourceMd);
                            _parseCache[sourceMd] = (doc, node);

                            while (_parseCache.Count > MaxCacheSize)
                            {
                                var last = _lruOrder.Last!;
                                _lruOrder.RemoveLast();
                                _parseCache.Remove(last.Value);
                            }
                        }
                    }

                    match = doc.Descendants<ExportBlock>().FirstOrDefault(e => impId.Equals(e.Id.ToString(), StringComparison.InvariantCulture));
                }
            }
            else match = availableExports?.FirstOrDefault(e => kvp.Key.Equals(e.Id.ToString(), StringComparison.InvariantCulture));

            if (match != null)
            {
                var importBlock = kvp.Value;
                renderer.Push(importBlock);
                renderer.WriteChildren(match);
                renderer.Pop(false);
            }
        }
        importBlocks.Clear();
    }
}
