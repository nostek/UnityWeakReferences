using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(UnityWeakReference), true)]
public class EditorUnityWeakReferenceDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		System.Type weakType =
			(fieldInfo.FieldType.IsArray)
			? ((UnityWeakReference[])fieldInfo.GetValue(property.serializedObject.targetObject))[0].GetWeakType()
			: ((UnityWeakReference)fieldInfo.GetValue(property.serializedObject.targetObject)).GetWeakType();

		var prefab = property.FindPropertyRelative("reference");
		prefab.objectReferenceValue = EditorGUI.ObjectField(position, label, prefab.objectReferenceValue, weakType, false);
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUIUtility.singleLineHeight;
	}
}
