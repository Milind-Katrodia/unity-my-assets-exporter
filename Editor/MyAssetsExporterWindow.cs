using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UnityMyAssetsExporter.Editor
{
    /// <summary>
    /// Open-source Unity Editor Tool to export entitlements from 'Package Manager -> My Assets'
    /// including Overview, Compatibility, Size, Store Link, and Publisher information to JSON and CSV.
    /// 
    /// License: MIT License
    /// Security & Privacy:
    /// - Runs entirely locally within the active Unity Editor process.
    /// - Inspects internal in-memory data structures loaded by Unity's Package Manager UI.
    /// - Does NOT upload, transmit, or share credentials, tokens, or asset lists to third-party servers.
    /// </summary>
    public class MyAssetsExporterWindow : EditorWindow
    {
        [Serializable]
        public class AssetMetadata
        {
            public string title;
            public string packageId;
            public string version;
            public string category;
            public string compatibility;
            public string size;
            public string author;
            public string publisherWebsite;
            public string storeLink;
            public string overview;
            public string status;
        }

        [Serializable]
        public class AssetExportReport
        {
            public string exportedAt;
            public string unityVersion;
            public int totalCount;
            public List<AssetMetadata> assets = new List<AssetMetadata>();
        }

        private Vector2 scrollPos;
        private string searchFilter = "";
        private List<AssetMetadata> loadedAssets = new List<AssetMetadata>();
        private string statusMessage = "Click 'Scan & Load My Assets' to read data from the Package Manager window.";

        [MenuItem("Tools/My Assets Exporter")]
        public static void ShowWindow()
        {
            var window = GetWindow<MyAssetsExporterWindow>("My Assets Exporter");
            window.minSize = new Vector2(650, 480);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Unity Package Manager: My Assets Exporter", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Instructions:\n" +
                "1. Open 'Window > Package Manager' and switch the dropdown to 'My Assets' (allow items to load).\n" +
                "2. Click 'Scan & Load My Assets' below.\n" +
                "3. Preview your asset list and click 'Export to JSON' or 'Export to CSV'.\n\n" +
                "Privacy Guarantee: 100% offline & local. No tokens or personal identifiers are stored or transmitted.",
                MessageType.Info);

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 Scan & Load My Assets", GUILayout.Height(34)))
            {
                ScanAndLoadAssets();
            }

            EditorGUI.BeginDisabledGroup(loadedAssets.Count == 0);
            if (GUILayout.Button($"💾 Export to CSV ({loadedAssets.Count})", GUILayout.Height(34)))
            {
                ExportCsv();
            }
            if (GUILayout.Button($"📄 Export to JSON ({loadedAssets.Count})", GUILayout.Height(34)))
            {
                ExportJson();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(statusMessage, MessageType.None);

            if (loadedAssets.Count > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Search / Filter:", GUILayout.Width(100));
                searchFilter = EditorGUILayout.TextField(searchFilter);
                if (GUILayout.Button("Clear", GUILayout.Width(50))) searchFilter = "";
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                int displayedCount = 0;
                foreach (var item in loadedAssets)
                {
                    if (!string.IsNullOrEmpty(searchFilter) &&
                        !item.title.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase).ToString().StartsWith("-1") &&
                        item.title.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) < 0 &&
                        item.category.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    displayedCount++;
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(item.title, EditorStyles.boldLabel);
                    if (!string.IsNullOrEmpty(item.version)) EditorGUILayout.LabelField($"v{item.version}", GUILayout.Width(80));
                    if (!string.IsNullOrEmpty(item.compatibility)) EditorGUILayout.LabelField($"Unity {item.compatibility}", GUILayout.Width(120));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    if (!string.IsNullOrEmpty(item.category)) EditorGUILayout.LabelField($"Category: {item.category}", EditorStyles.miniLabel);
                    if (!string.IsNullOrEmpty(item.size)) EditorGUILayout.LabelField($"Size: {item.size}", EditorStyles.miniLabel, GUILayout.Width(120));
                    if (!string.IsNullOrEmpty(item.author)) EditorGUILayout.LabelField($"Author: {item.author}", EditorStyles.miniLabel, GUILayout.Width(160));
                    EditorGUILayout.EndHorizontal();

                    if (!string.IsNullOrEmpty(item.storeLink))
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"Store: {item.storeLink}", EditorStyles.miniLabel);
                        if (GUILayout.Button("Open Store Page", GUILayout.Width(110)))
                        {
                            Application.OpenURL(item.storeLink);
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndVertical();

                    if (displayedCount >= 100)
                    {
                        EditorGUILayout.HelpBox($"Showing first 100 items matching filter. Export contains all {loadedAssets.Count} items.", MessageType.Info);
                        break;
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        public void ScanAndLoadAssets()
        {
            loadedAssets = ExtractPackagesInternal();
            if (loadedAssets.Count > 0)
            {
                statusMessage = $"Successfully loaded {loadedAssets.Count} assets from Package Manager! Ready to export.";
            }
            else
            {
                statusMessage = "No assets found. Please open 'Window > Package Manager', switch the dropdown to 'My Assets', and wait a moment for items to display before scanning.";
            }
            Repaint();
        }

        public static List<AssetMetadata> ExtractPackagesInternal()
        {
            var results = new List<AssetMetadata>();
            var seenIds = new HashSet<string>();

            var ws = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
            EditorWindow pm = null;
            for (int i = 0; i < ws.Length; i++)
            {
                if (ws[i].GetType().Name == "PackageManagerWindow") { pm = (EditorWindow)ws[i]; break; }
            }
            if (pm == null) return results;

            BindingFlags allFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var mRoot = pm.GetType().GetField("m_Root", allFlags)?.GetValue(pm);
            var pkgDb = mRoot?.GetType().GetField("m_PackageDatabase", allFlags)?.GetValue(mRoot);
            if (pkgDb == null) return results;

            var fields = pkgDb.GetType().GetFields(allFlags);
            foreach (var f in fields)
            {
                var val = f.GetValue(pkgDb);
                if (val is IEnumerable enumerable && !(val is string))
                {
                    foreach (var rawItem in enumerable)
                    {
                        object item = rawItem;
                        if (rawItem.GetType().Name.StartsWith("KeyValuePair"))
                        {
                            item = rawItem.GetType().GetProperty("Value")?.GetValue(rawItem);
                        }
                        if (item == null) continue;

                        string id = GetVal(item, "uniqueId") ?? GetVal(item, "packageId") ?? GetVal(item, "name");
                        string title = GetVal(item, "displayName") ?? GetVal(item, "name") ?? GetVal(item, "title");
                        if (string.IsNullOrEmpty(title)) continue;

                        string key = id + "_" + title;
                        if (seenIds.Contains(key)) continue;
                        seenIds.Add(key);

                        var meta = new AssetMetadata
                        {
                            title = title,
                            packageId = id
                        };

                        // Extract product metadata (store link, description/overview)
                        object product = GetObj(item, "product");
                        string productUrl = null;
                        string overview = null;
                        if (product != null)
                        {
                            productUrl = GetVal(product, "productUrl");
                            overview = GetVal(product, "description");
                        }

                        if (string.IsNullOrEmpty(productUrl) && !string.IsNullOrEmpty(id) && long.TryParse(id, out _))
                        {
                            productUrl = $"https://assetstore.unity.com/packages/slug/{id}";
                        }
                        meta.storeLink = productUrl ?? "";
                        meta.overview = overview ?? GetVal(item, "description") ?? "";

                        // Extract versions, compatibility, size, author, category
                        object versionList = GetObj(item, "versions");
                        string verStr = null;
                        string authorStr = null;
                        string categoryStr = null;
                        string compatibility = null;
                        string sizeStr = null;

                        if (versionList is IEnumerable vEnum && !(versionList is string))
                        {
                            foreach (var v in vEnum)
                            {
                                if (v == null) continue;
                                verStr = GetVal(v, "versionString") ?? GetVal(v, "version");
                                authorStr = GetVal(v, "author");
                                categoryStr = GetVal(v, "category");
                                compatibility = GetVal(v, "supportedVersion") ?? GetVal(v, "supportedUnityVersionString");

                                object sizeInfos = GetObj(v, "sizes") ?? GetObj(v, "m_SizeInfos");
                                if (sizeInfos is IEnumerable sEnum)
                                {
                                    var sizeParts = new List<string>();
                                    foreach (var s in sEnum)
                                    {
                                        if (s == null) continue;
                                        string sz = FormatSizeObject(s);
                                        if (!string.IsNullOrEmpty(sz)) sizeParts.Add(sz);
                                    }
                                    if (sizeParts.Count > 0) sizeStr = string.Join("; ", sizeParts);
                                }
                                break;
                            }
                        }

                        meta.version = verStr ?? GetVal(item, "version") ?? "";
                        meta.author = authorStr ?? GetVal(item, "author") ?? "";
                        meta.category = categoryStr ?? GetVal(item, "category") ?? "";
                        meta.compatibility = compatibility ?? "";
                        meta.size = sizeStr ?? "";
                        meta.publisherWebsite = !string.IsNullOrEmpty(overview) ? ExtractPublisherWebsite(overview, productUrl) : "";
                        meta.status = GetVal(item, "state") ?? GetVal(item, "status") ?? "";

                        results.Add(meta);
                    }
                }
            }

            return results;
        }

        private void ExportCsv()
        {
            string path = EditorUtility.SaveFilePanel("Export My Assets to CSV", Application.dataPath, "MyAssets_Export.csv", "csv");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            sb.AppendLine("Title,PackageId,Version,Category,Compatibility,Size,Author,PublisherWebsite,StoreLink,Overview,Status");

            foreach (var a in loadedAssets)
            {
                sb.AppendLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\"",
                    Escape(a.title),
                    Escape(a.packageId),
                    Escape(a.version),
                    Escape(a.category),
                    Escape(a.compatibility),
                    Escape(a.size),
                    Escape(a.author),
                    Escape(a.publisherWebsite),
                    Escape(a.storeLink),
                    Escape(a.overview),
                    Escape(a.status)
                ));
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(path);
            statusMessage = $"Exported {loadedAssets.Count} assets to CSV:\n{path}";
        }

        private void ExportJson()
        {
            string path = EditorUtility.SaveFilePanel("Export My Assets to JSON", Application.dataPath, "MyAssets_Export.json", "json");
            if (string.IsNullOrEmpty(path)) return;

            var report = new AssetExportReport
            {
                exportedAt = DateTime.UtcNow.ToString("o"),
                unityVersion = Application.unityVersion,
                totalCount = loadedAssets.Count,
                assets = loadedAssets
            };

            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(path, json, Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(path);
            statusMessage = $"Exported {loadedAssets.Count} assets to JSON:\n{path}";
        }

        private static string FormatSizeObject(object s)
        {
            if (s == null) return "";
            foreach (var propName in new[] { "formattedSize", "sizeString", "displaySize" })
            {
                var val = GetVal(s, propName);
                if (!string.IsNullOrEmpty(val)) return val;
            }

            foreach (var numName in new[] { "size", "bytes", "byteSize", "value", "m_Size", "m_Bytes" })
            {
                var obj = GetObj(s, numName);
                if (obj != null && long.TryParse(obj.ToString(), out long bytes) && bytes > 0)
                {
                    if (bytes < 1024) return $"{bytes} B";
                    if (bytes < 1024 * 1024) return $"{(bytes / 1024.0):F2} KB";
                    if (bytes < 1024 * 1024 * 1024) return $"{(bytes / (1024.0 * 1024.0)):F2} MB";
                    return $"{(bytes / (1024.0 * 1024.0 * 1024.0)):F2} GB";
                }
            }
            return "";
        }

        private static string ExtractPublisherWebsite(string text, string ignoreUrl)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var matches = Regex.Matches(text, @"https?://[^\s)\]""'>,;]+");
            foreach (Match m in matches)
            {
                string url = m.Value.TrimEnd('.', ',', ';', ')', ']');
                if (string.IsNullOrEmpty(url)) continue;
                if (!string.IsNullOrEmpty(ignoreUrl) && url.Equals(ignoreUrl, StringComparison.OrdinalIgnoreCase)) continue;
                if (url.Contains("unity3d.com") || url.Contains("unity.com") || url.Contains("imgur.com")) continue;
                return url;
            }
            return "";
        }

        private static object GetObj(object obj, string memberName)
        {
            if (obj == null) return null;
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
            var prop = obj.GetType().GetProperty(memberName, flags);
            if (prop != null)
            {
                try { return prop.GetValue(obj); } catch { }
            }
            var f = obj.GetType().GetField(memberName, flags);
            if (f != null)
            {
                try { return f.GetValue(obj); } catch { }
            }
            return null;
        }

        private static string GetVal(object obj, string memberName)
        {
            var res = GetObj(obj, memberName);
            return res != null ? res.ToString() : null;
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", "");
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
        }
    }
}
