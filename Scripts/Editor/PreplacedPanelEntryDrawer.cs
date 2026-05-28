using UnityEditor;
using UnityEngine;
using VoyageForge.UIKit.Runtime;

namespace VoyageForge.UIKit.Editor
{
    [CustomPropertyDrawer(typeof(PreplacedPanelEntry))]
    public class PreplacedPanelEntryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var panelProp = property.FindPropertyRelative("Panel");
            var keyProp = property.FindPropertyRelative("PanelKey");

            var lineH = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            // ---- Panel 字段 ----
            var panelRect = new Rect(position.x, position.y, position.width, lineH);
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(panelRect, panelProp, new GUIContent("Panel"));
            var panelChanged = EditorGUI.EndChangeCheck();

            // ---- PanelKey 字段 + Fill 按钮 ----
            var keyRect = new Rect(position.x, position.y + lineH + spacing, position.width - 80, lineH);
            var btnRect = new Rect(position.x + position.width - 75, keyRect.y, 75, lineH);

            var panel = panelProp.objectReferenceValue as BasePanel;
            var goName = panel != null ? panel.name : "";

            // 拖入 Panel 后 Key 为空 → 自动填入 GO 名称
            if (panelChanged && panel != null && string.IsNullOrEmpty(keyProp.stringValue))
            {
                keyProp.stringValue = goName;
            }

            EditorGUI.PropertyField(keyRect, keyProp, new GUIContent("Panel Key"));

            // Fill 按钮
            var oldEnabled = GUI.enabled;
            GUI.enabled = panel != null;
            if (GUI.Button(btnRect, "Fill Name"))
            {
                keyProp.stringValue = goName;
            }
            GUI.enabled = oldEnabled;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
        }
    }
}
