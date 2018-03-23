using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(UnityWeakReference), true)]
public class EditorUnityWeakReferenceDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		var obj = PropertyPath(property, fieldInfo);

		var weakType = typeof(UnityEngine.GameObject);

		if (obj is System.Collections.IList)
		{
			var list = (obj as System.Collections.IList);
			if (list.Count > 0)
				weakType = (list[0] as UnityWeakReference).GetWeakType();
		}
		else if (obj.GetType().IsArray)
		{
			var arr = (UnityWeakReference[])obj;
			if (arr.Length > 0)
				weakType = arr[0].GetWeakType();
		}
		else
			weakType = ((UnityWeakReference)obj).GetWeakType();

		var prefab = property.FindPropertyRelative("reference");
		prefab.objectReferenceValue = EditorGUI.ObjectField(position, label, prefab.objectReferenceValue, weakType, false);
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUIUtility.singleLineHeight;
	}

	static System.Object PropertyPath(SerializedProperty property, System.Reflection.FieldInfo fieldInfo)
	{
		System.Object prop = property.serializedObject.targetObject;

		string[] path = property.propertyPath.Split('.');

		for (int i = 0; i < path.Length; i++)
		{
			string node = path[i];

			if (i == path.Length - 2 && node == "Array" && path[i + 1].StartsWith("data["))
				break;

			prop = prop
				.GetType()
				.GetField(node, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
				.GetValue(prop);
		}

		return prop;
	}
}
