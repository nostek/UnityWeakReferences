using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(UnityWeakReference), true)]
public class EditorUnityWeakReferenceDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		UnityWeakReference obj = (UnityWeakReference)fieldInfo.GetValue(property.serializedObject.targetObject);

		var prefab = property.FindPropertyRelative("reference");
		prefab.objectReferenceValue = EditorGUI.ObjectField(position, label, prefab.objectReferenceValue, obj.GetWeakType(), false);
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUIUtility.singleLineHeight;
	}
}
