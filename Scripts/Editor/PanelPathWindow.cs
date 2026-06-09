using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private string _filterGroup = "";

        [MenuItem("Tools/UIKit/Panel Path Window")]
        public static void Open() => GetWindow<PanelPathWindow>("Panel Path");

        private void OnEnable() => RestoreEntries();
        private void OnDisable() => SaveEntries();

        private void CreateGUI()
        {
            var root = rootVisualElement;

            // 拖放区
            var dropZone = new VisualElement { style = { marginBottom = 4 } };
            dropZone.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            dropZone.RegisterCallback<DragPerformEvent>(OnDragPerform);
            var dropLabel = new Label("从 Project 拖入 BasePanel Prefab") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            dropZone.Add(dropLabel);
            root.Add(dropZone);

            // 工具栏
            var toolbar = new Toolbar();
            toolbar.Add(new ToolbarButton(() => { _entries.Add(new PanelPathEntry()); _listView.Rebuild(); SaveEntries(); })
                { text = "添加行" });
            toolbar.Add(new ToolbarButton(ApplyAll) { text = "全部应用" });
            root.Add(toolbar);

            // 列表
            _listView = new ListView(_entries, 24, MakeItem, BindItem)
            {
                selectionType = SelectionType.Multiple,
                reorderable = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
            };
            root.Add(_listView);
        }

        // ---- 批量拖放 ----

        private void OnDragUpdate(DragUpdatedEvent evt)
        {
            if (DragAndDrop.objectReferences.OfType<GameObject>().Any(g => g.GetComponent<BasePanel>() != null))
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            foreach (var go in DragAndDrop.objectReferences.OfType<GameObject>())
            {
                if (go.GetComponent<BasePanel>() == null) continue;
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go))) continue;
                if (_entries.Any(e => e.Prefab == go)) continue;
                _entries.Add(new PanelPathEntry { Prefab = go });
            }
            _listView.Rebuild();
            SaveEntries();
        }

        // ---- 行构建 ----

        private VisualElement MakeItem()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            var prefabField = new ObjectField { objectType = typeof(GameObject), allowSceneObjects = false, name = "prefab" };
            prefabField.RegisterValueChangedCallback(OnPrefabChanged);
            prefabField.style.flexGrow = 1;
            row.Add(prefabField);

            var statusLabel = new Label { name = "status", style = { width = 24, unityTextAlign = TextAnchor.MiddleCenter } };
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
            el.Q<Label>("status").text = entry.Applied ? "✓" : "○";
            el.Q<Label>("status").style.color = entry.Applied ? new Color(0.2f, 0.7f, 0.2f) : Color.gray;
            el.Q<Button>("apply").userData = i;
            el.Q<Button>("remove").userData = i;
        }

        private static void OnPrefabChanged(ChangeEvent<Object> evt)
        {
            var go = evt.newValue as GameObject;
            if (go == null) return;
            if (go.GetComponent<BasePanel>() == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go)))
                (evt.target as ObjectField).value = null;
        }

        // ---- 应用逻辑 ----

        private void ApplySingle(int i)
        {
            var e = _entries[i];
            if (e.Prefab == null) return;
            var panel = e.Prefab.GetComponent<BasePanel>();
            var path = GetResourcesPath(e.Prefab);
            if (panel != null && path != null)
            {
                ApplyAttribute(panel.GetType(), path);
                e.Applied = true;
                _listView.Rebuild();
                SaveEntries();
            }
        }

        private void ApplySelected()
        {
            foreach (var i in _listView.selectedIndices)
                ApplySingle(i);
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
                e.Applied = true;
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
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.GetComponent<BasePanel>() != null)
                    _entries.Add(new PanelPathEntry { Prefab = prefab, Applied = true });
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

        private static string GetGroup(string resPath) =>
            resPath?.Contains('/') == true ? resPath.Substring(0, resPath.LastIndexOf('/')) : "/";

        private static void ApplyAttribute(System.Type type, string resPath)
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

            content = Regex.Replace(content, @"\[PanelPath\([^)]*\)\]\s*\n\s*", "");
            var pattern = $"(class\\s+{className}\\s*:)";
            content = Regex.Replace(content, pattern, $"{attr}\n    class {className} :");

            File.WriteAllText(scriptPath, content);
            AssetDatabase.Refresh();
            Debug.Log($"[UIKit] PanelPath applied: {className} → {resPath}");
        }

        [System.Serializable] private class GuidList { public List<string> Items = new(); }
        [System.Serializable] private class PanelPathEntry { public GameObject Prefab; public bool Applied; }
    }
}
