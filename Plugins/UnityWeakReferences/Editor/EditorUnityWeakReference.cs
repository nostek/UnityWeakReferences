using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine.SceneManagement;
using UnityEditor.Build.Reporting;

public sealed class EditorUnityWeakReference : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
	class AssetInfo
	{
		public string GUID;
		public string Path;
		public string WeakPath;
		public System.Type Type;
	}

	public int callbackOrder { get { return 0; } }

	public void OnPreprocessBuild(BuildReport report)
	{
		PrepareFolder();

		Dictionary<string, System.Type> validTypes = new Dictionary<string, System.Type>();
		var types = System.Reflection.Assembly.GetAssembly(typeof(UnityWeakReference)).GetTypes();
		foreach (var type in types)
		{
			if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(UnityWeakReference)))
			{
				var otype = (type.GetCustomAttributes(typeof(UnityWeakReferenceType), false)[0] as UnityWeakReferenceType).Type;
				validTypes.Add(type.Name, otype);
			}
		}

		Dictionary<string, AssetInfo> assetsToReference = new Dictionary<string, AssetInfo>();

		var guids = AssetDatabase.FindAssets("t:Prefab");
		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);

			DoWork(go, assetsToReference, validTypes);
		}

		guids = AssetDatabase.FindAssets("t:ScriptableObject");
		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var sos = AssetDatabase.LoadAllAssetsAtPath(path);

			foreach (var so in sos)
				if (so is ScriptableObject)
				{
					DoWork(so, assetsToReference, validTypes);
				}
		}

		for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCountInBuildSettings; i++)
		{
			var scenePath = SceneUtility.GetScenePathByBuildIndex(i);

			if (string.IsNullOrEmpty(scenePath))
				continue;

			var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

			var objs = scene.GetRootGameObjects();
			foreach (var go in objs)
				DoWork(go, assetsToReference, validTypes);
		}

		try
		{
			AssetDatabase.StartAssetEditing();

			foreach (var a in assetsToReference)
				CreateReferenceFile(a.Value);
		}
		finally
		{
			AssetDatabase.StopAssetEditing();
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
	}

	public void OnPostprocessBuild(BuildReport report)
	{
		PrepareFolder(false);
	}

	/////////////////////

	static void DoWork(GameObject go, Dictionary<string, AssetInfo> assetsToReference, Dictionary<string, System.Type> validTypes)
	{
		if (go == null)
			return;

		var behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);

		foreach (var b in behaviours)
			DoWork(b, assetsToReference, validTypes);
	}

	static void DoWork(UnityEngine.Object obj, Dictionary<string, AssetInfo> assetsToReference, Dictionary<string, System.Type> validTypes)
	{
		if (obj == null)
			return;

		var so = new SerializedObject(obj);
		var itr = so.GetIterator();
		while (itr.NextVisible(true))
		{
			if (itr.propertyType == SerializedPropertyType.Generic && validTypes.ContainsKey(itr.type))
			{
				if(!itr.isArray)
               			{
					var pref = itr.FindPropertyRelative("reference");
					var ppath = itr.FindPropertyRelative("path");
					var pgo = pref.objectReferenceValue;

					if (pgo == null)
						continue;

					var path = AssetDatabase.GetAssetPath(pgo);
					var guid = AssetDatabase.AssetPathToGUID(path);

					var pt = validTypes[itr.type];

					if (!assetsToReference.ContainsKey(guid))
						assetsToReference.Add(guid, new AssetInfo { GUID = guid, Path = path, WeakPath = ppath.stringValue, Type = pt });
				}
				else
				{
					// drill down into arrays
					for(int i = 0; i < itr.arraySize; i++)
                    			{
						var element = itr.GetArrayElementAtIndex(i);
						DoWork(element.serializedObject.targetObject, assetsToReference, validTypes);
					}
				}
			}
		}
	}

	static void PrepareFolder(bool create = true)
	{
		if (create && !AssetDatabase.IsValidFolder("Assets/Resources"))
		{
			AssetDatabase.CreateFolder("Assets", "Resources");
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
		}

		if (AssetDatabase.IsValidFolder("Assets/Resources/_GeneratedWeaks_"))
		{
			FileUtil.DeleteFileOrDirectory("Assets/Resources/_GeneratedWeaks_");
			FileUtil.DeleteFileOrDirectory("Assets/Resources/_GeneratedWeaks_.meta");
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
		}

		if (create)
		{
			AssetDatabase.CreateFolder("Assets/Resources", "_GeneratedWeaks_");
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

			AssetDatabase.CreateFolder("Assets/Resources/_GeneratedWeaks_", "0");
			AssetDatabase.CreateFolder("Assets/Resources/_GeneratedWeaks_", "1");
			AssetDatabase.CreateFolder("Assets/Resources/_GeneratedWeaks_", "2");
			AssetDatabase.CreateFolder("Assets/Resources/_GeneratedWeaks_", "3");
			AssetDatabase.CreateFolder("Assets/Resources/_GeneratedWeaks_", "4");
			AssetDatabase.CreateFolder("Assets/Resources/_GeneratedWeaks_", "5");
			AssetDatabase.CreateFolder("Assets/Resources/_GeneratedWeaks_", "6");
			AssetDatabase.CreateFolder("Assets/Resources/_GeneratedWeaks_", "7");
			AssetDatabase.CreateFolder("Assets/Resources/_GeneratedWeaks_", "8");
			AssetDatabase.CreateFolder("Assets/Resources/_GeneratedWeaks_", "9");
			AssetDatabase.CreateFolder("Assets/Resources/_GeneratedWeaks_", "a");
			AssetDatabase.CreateFolder("Assets/Resources/_GeneratedWeaks_", "b");
			AssetDatabase.CreateFolder("Assets/Resources/_GeneratedWeaks_", "c");
			AssetDatabase.CreateFolder("Assets/Resources/_GeneratedWeaks_", "d");
			AssetDatabase.CreateFolder("Assets/Resources/_GeneratedWeaks_", "e");
			AssetDatabase.CreateFolder("Assets/Resources/_GeneratedWeaks_", "f");
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
		}
	}

	static void CreateReferenceFile(AssetInfo info)
	{
		if (string.IsNullOrEmpty(info.GUID))
			return;

		string src = info.Path;

		string weakdest = string.Format("_GeneratedWeaks_/{0}/{1}_{2}", info.GUID[0], info.GUID, FilterPath(src));
		UnityEngine.Assertions.Assert.IsTrue(weakdest == info.WeakPath, $"Mismatch in Weak Path and Destination Path. {info.WeakPath} <> {weakdest}");

		string dest = string.Format("Assets/Resources/_GeneratedWeaks_/{0}/{1}_{2}.asset", info.GUID[0], info.GUID, FilterPath(src));

		var obj = AssetDatabase.LoadAssetAtPath(src, info.Type);

		var so = ScriptableObject.CreateInstance<UnityWeakReferenceScriptableObject>();
		so.UnityObject = obj;

#if !UNITY_CLOUD_BUILD
		Debug.LogFormat("[UnityWeakReference] Created {0} -> {1}", src, dest);
#endif

		AssetDatabase.CreateAsset(so, dest);
	}

	static string FilterPath(string path)
	{
		path = path.Substring(0, path.LastIndexOf("."));
		path = path.Replace('.', '_');
		path = path.Replace('/', '_');
		path = path.Replace(' ', '_');
		return path;
	}
}
