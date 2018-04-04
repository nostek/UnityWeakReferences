using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(UnityWeakReference), true)]
public class EditorUnityWeakReferenceDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		var weakType = GetWeakType(fieldInfo);

		var prefab = property.FindPropertyRelative("reference");
		prefab.objectReferenceValue = EditorGUI.ObjectField(position, label, prefab.objectReferenceValue, weakType, false);
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUIUtility.singleLineHeight;
	}

	static System.Type GetWeakType(System.Reflection.FieldInfo fieldInfo)
	{
		System.Type baseType = fieldInfo.FieldType;

		if (fieldInfo.FieldType.IsArray)
			baseType = fieldInfo.FieldType.GetElementType();

		if (fieldInfo.FieldType.IsGenericType)
			baseType = fieldInfo.FieldType.GetGenericArguments()[0];

		return (baseType.GetCustomAttributes(typeof(UnityWeakReferenceType), false)[0] as UnityWeakReferenceType).Type;
	}
}
