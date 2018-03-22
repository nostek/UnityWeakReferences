using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(UnityWeakReference), true)]
public class EditorUnityWeakReferenceDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		var obj = fieldInfo.GetValue(property.serializedObject.targetObject);

		var weakType = default(System.Type);

		if (obj is System.Collections.IList)
			weakType = (((obj as System.Collections.IList)[0]) as UnityWeakReference).GetWeakType();
		else if (obj.GetType().IsArray)
			weakType = ((UnityWeakReference[])obj)[0].GetWeakType();
		else
			weakType = ((UnityWeakReference)obj).GetWeakType();

		var prefab = property.FindPropertyRelative("reference");
		prefab.objectReferenceValue = EditorGUI.ObjectField(position, label, prefab.objectReferenceValue, weakType, false);
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUIUtility.singleLineHeight;
	}
}
