using Point.Collections.Editor;
using UnityEditor;
using UnityEngine;

namespace Point.Audio.FMODEditor
{
    //[CustomPropertyDrawer(typeof(AudioReference), true)]
    //public sealed class AudioReferencePropertyDrawer : PropertyDrawer
    //{
    //    private sealed class Helper
    //    {
    //        const string
    //            c_Event = "m_Event",
    //            c_Parameters = "m_Parameters";

    //        private static GUIContent
    //            EventContent = new GUIContent("Event",
    //                ""),
    //            ParametersContent = new GUIContent("Parameters",
    //                "");

    //        public static SerializedProperty GetEventField(SerializedProperty property)
    //            => property.FindPropertyRelative(c_Event);
    //        public static SerializedProperty GetParametersField(SerializedProperty property)
    //            => property.FindPropertyRelative(c_Parameters);
    //    }

    //    //public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    //    //{
    //    //    if (!property.isExpanded) return PropertyDrawerHelper.GetPropertyHeight(1);

    //    //    return PropertyDrawerHelper.GetPropertyHeight(3);
    //    //}
    //    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    //    {
    //        if (string.IsNullOrEmpty(label.text)) label = new GUIContent(property.displayName);

    //        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
    //        if (!property.isExpanded) return;
    //        EditorGUI.indentLevel++;

    //        PropertyDrawerHelper.Space(ref position);

    //        using (new EditorUtilities.BoxBlock(Color.black))
    //        using (var change = new EditorGUI.ChangeCheckScope())
    //        using (new EditorGUI.PropertyScope(position, null, property))
    //        {
    //            var eventField = Helper.GetEventField(property);
    //            EditorGUI.PropertyField(PropertyDrawerHelper.GetRect(position), eventField);

    //            PropertyDrawerHelper.Space(ref position);
    //            var parameters = Helper.GetParametersField(property);
    //            EditorGUI.PropertyField(position, parameters);

    //            if (change.changed)
    //            {
    //                property.serializedObject.ApplyModifiedProperties();
    //            }
    //        }

    //        EditorGUI.indentLevel--;
    //    }
    //}
}
