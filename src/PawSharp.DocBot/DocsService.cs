using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;

namespace PawSharp.DocBot
{
    public class DocEntry
    {
        public string? Id { get; set; }
        public string? Kind { get; set; }
        public string? Name { get; set; }
        public string? Summary { get; set; }
        public string? Remarks { get; set; }
        public string? Returns { get; set; }
    }

    public class DocsService
    {
        private readonly List<DocEntry> _entries = new();

        public DocsService(string referenceFolder)
        {
            Load(referenceFolder);
        }

        private void Load(string referenceFolder)
        {
            if (!Directory.Exists(referenceFolder)) return;
            var indexPath = Path.Combine(referenceFolder, "index.json");
            if (!File.Exists(indexPath)) return;
            var index = JsonSerializer.Deserialize<List<JsonElement>>(File.ReadAllText(indexPath));
            foreach (var item in index)
            {
                if (!item.TryGetProperty("file", out var f)) continue;
                var file = f.GetString();
                var full = Path.Combine(referenceFolder, file);
                if (!File.Exists(full)) continue;
                using var fs = File.OpenRead(full);
                using var doc = JsonDocument.Parse(fs);
                if (!doc.RootElement.TryGetProperty("members", out var members)) continue;
                foreach (var m in members.EnumerateArray())
                {
                    var entry = new DocEntry
                    {
                        Id = m.GetProperty("id").GetString(),
                        Kind = m.GetProperty("kind").GetString(),
                        Name = m.GetProperty("name").GetString(),
                        Summary = m.GetProperty("summary").GetString(),
                        Remarks = m.GetProperty("remarks").GetString(),
                        Returns = m.GetProperty("returns").GetString()
                    };
                    _entries.Add(entry);
                }
            }
        }

        public DocEntry? Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;
            query = query.ToLowerInvariant();
            // simple scoring: name exact -> summary contains
            var byName = _entries.FirstOrDefault(e => e.Name?.ToLowerInvariant().Contains(query) == true);
            if (byName != null) return byName;
            var bySummary = _entries.FirstOrDefault(e => e.Summary?.ToLowerInvariant().Contains(query) == true);
            return bySummary;
        }
    }
}
