using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Editor
{
    public class PanelPathWindow : EditorWindow
    {
        [SerializeField] private List<PanelPathEntry> _entries = new();
        private ListView _listView;

        [MenuItem("Tools/UIKit/Panel Path Window")]
        public static void Open() => GetWindow<PanelPathWindow>("Panel Path");

        private void OnEnable() => RestoreEntries();
        private void OnDisable() => SaveEntries();

        private void CreateGUI()
        {
            var root = rootVisualElement;

            var toolbar = new Toolbar();
            toolbar.Add(new ToolbarButton(() => { _entries.Add(new PanelPathEntry()); _listView.Rebuild(); SaveEntries(); })
                { text = "添加行" });
            toolbar.Add(new ToolbarButton(ApplyAll) { text = "全部应用" });
            root.Add(toolbar);

            _listView = new ListView(_entries, 24, MakeItem, BindItem)
            {
                selectionType = SelectionType.Multiple,
                reorderable = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
            };
            root.Add(_listView);
        }

        // ---- 行 ----

        private VisualElement MakeItem()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            var prefabField = new ObjectField { objectType = typeof(GameObject), allowSceneObjects = false, name = "prefab" };
            prefabField.RegisterValueChangedCallback(evt =>
            {
                var go = evt.newValue as GameObject;
                if (go == null) return;
                if (go.GetComponent<BasePanel>() == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go)))
                    (evt.target as ObjectField).value = null;
            });
            prefabField.style.flexGrow = 1;
            row.Add(prefabField);

            var statusLabel = new Label { name = "status", style = { unityTextAlign = TextAnchor.MiddleCenter } };
            row.Add(statusLabel);

            var applyBtn = new Button { text = "应用", name = "apply", style = { width = 48 } };
            applyBtn.clicked += () => ApplySingle((int)applyBtn.userData);
            row.Add(applyBtn);

            var removeBtn = new Button { text = "×", name = "remove", style = { width = 24 } };
            removeBtn.clicked += () => { _entries.RemoveAt((int)removeBtn.userData); _listView.Rebuild(); SaveEntries(); };
            row.Add(removeBtn);

            return row;
        }

        private void BindItem(VisualElement el, int i)
        {
            var entry = _entries[i];
            el.Q<ObjectField>("prefab").SetValueWithoutNotify(entry.Prefab);

            var label = el.Q<Label>("status");
            if (entry.Prefab != null && entry.Prefab.GetComponent<BasePanel>() != null)
            {
                var type = entry.Prefab.GetComponent<BasePanel>().GetType();
                var expected = GetResourcesPath(entry.Prefab);
                var current = type.GetCustomAttribute<PanelPathAttribute>()?.Path;

                if (current == expected)      { label.text = "✓"; label.tooltip = current; }
                else if (current != null)      { label.text = "⚠"; label.tooltip = $"{current} → {expected}"; }
                else if (expected != null)      { label.text = "+"; label.tooltip = $"添加: {expected}"; }
                else                           { label.text = "·"; label.tooltip = "不在 Resources 下"; }
            }
            else { label.text = "-"; label.tooltip = ""; }

            el.Q<Button>("apply").userData = i;
            el.Q<Button>("remove").userData = i;
        }

        // ---- 应用 ----

        private void ApplySingle(int i)
        {
            var e = _entries[i];
            if (e.Prefab == null) return;
            var panel = e.Prefab.GetComponent<BasePanel>();
            var path = GetResourcesPath(e.Prefab);
            if (panel != null && path != null)
            {
                ApplyAttribute(panel.GetType(), path);
                _listView.Rebuild();
                SaveEntries();
            }
        }

        private void ApplyAll()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e.Prefab == null) continue;
                var panel = e.Prefab.GetComponent<BasePanel>();
                var path = GetResourcesPath(e.Prefab);
                if (panel == null || path == null) continue;
                ApplyAttribute(panel.GetType(), path);
            }
            _listView.Rebuild();
            SaveEntries();
        }

        // ---- 持久化 ----

        private void RestoreEntries()
        {
            var json = EditorPrefs.GetString("UIKit_PanelPath_Entries", "[]");
            var guids = JsonUtility.FromJson<GuidList>(json);
            _entries.Clear();
            foreach (var guid in guids.Items)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null && prefab.GetComponent<BasePanel>() != null)
                    _entries.Add(new PanelPathEntry { Prefab = prefab });
            }
        }

        private void SaveEntries()
        {
            var guids = new List<string>();
            foreach (var e in _entries)
            {
                if (e.Prefab == null) continue;
                var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(e.Prefab));
                if (!string.IsNullOrEmpty(guid)) guids.Add(guid);
            }
            EditorPrefs.SetString("UIKit_PanelPath_Entries", JsonUtility.ToJson(new GuidList { Items = guids }));
        }

        // ---- 辅助 ----

        private static string GetResourcesPath(GameObject prefab)
        {
            var assetPath = AssetDatabase.GetAssetPath(prefab);
            var match = Regex.Match(assetPath, @"Resources/(.+)\.prefab$");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static void ApplyAttribute(Type type, string resPath)
        {
            var guids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");
            string scriptPath = null;
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p.EndsWith($"{type.Name}.cs")) { scriptPath = p; break; }
            }
            if (scriptPath == null) return;

            var content = File.ReadAllText(scriptPath);
            var className = type.Name;
            var attr = $"[PanelPath(\"{resPath}\")]";

            if (content.Contains(attr)) return;

            // 替换或插入
            content = Regex.Replace(content, @"\[PanelPath\([^)]*\)\]\s*\n\s*", "");
            content = Regex.Replace(content, $"(class\\s+{className}\\s*:)",
                $"{attr}\n    class {className} :");

            File.WriteAllText(scriptPath, content);
            AssetDatabase.Refresh();
            Debug.Log($"[UIKit] PanelPath applied: {className} → {resPath}");
        }

        [Serializable] private class GuidList { public List<string> Items = new(); }
        [Serializable] private class PanelPathEntry { public GameObject Prefab; }
    }
}
