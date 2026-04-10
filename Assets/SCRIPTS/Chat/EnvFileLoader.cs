using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Reads a <c>.env</c> file from the project/build root and exposes key-value pairs.
/// Supports <c>KEY=value</c>, optional single/double quotes, blank lines, and <c>#</c> comments.
/// </summary>
public static class EnvFileLoader
{
    static readonly Dictionary<string, string> _vars = new();
    static bool _loaded;

    /// <summary>
    /// Resolves the <c>.env</c> path next to the project root (Editor) or beside the
    /// executable's data folder (standalone build), then parses it once.
    /// </summary>
    public static void Load()
    {
        if (_loaded) return;
        _loaded = true;

        // Application.dataPath:
        //   Editor  → <project>/Assets
        //   Build   → <exe>/<product>_Data
        // ".." lands on the repo / executable root in both cases.
        string envPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".env"));

        if (!File.Exists(envPath))
        {
            Debug.LogWarning($"[EnvFileLoader] .env not found at {envPath}");
            return;
        }

        foreach (string raw in File.ReadAllLines(envPath))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;

            int eq = line.IndexOf('=');
            if (eq < 1) continue;

            string key = line.Substring(0, eq).Trim();
            string val = line.Substring(eq + 1).Trim();

            if (val.Length >= 2 &&
                ((val[0] == '"' && val[val.Length - 1] == '"') ||
                 (val[0] == '\'' && val[val.Length - 1] == '\'')))
            {
                val = val.Substring(1, val.Length - 2);
            }

            _vars[key] = val;
        }
    }

    public static string Get(string key, string fallback = "")
    {
        Load();
        return _vars.TryGetValue(key, out string v) ? v : fallback;
    }

    public static bool TryGet(string key, out string value)
    {
        Load();
        return _vars.TryGetValue(key, out value);
    }
}
