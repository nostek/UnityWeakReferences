using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine.SceneManagement;
using UnityEditor.Build.Reporting;

public sealed class EditorUnityWeakReference : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
	struct AssetInfo
	{
		public string GUID;
		public System.Type Type;
		public string Path;
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

		foreach (var a in assetsToReference)
			CreateReferenceFile(a.Value);

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
			if (itr.propertyType == SerializedPropertyType.Generic && !itr.isArray && validTypes.ContainsKey(itr.type))
			{
				var pref = itr.FindPropertyRelative("reference");
				var pgo = pref.objectReferenceValue;

				if (pgo == null)
					continue;

				var pt = validTypes[itr.type];

				var path = AssetDatabase.GetAssetPath(pgo);
				var guid = AssetDatabase.AssetPathToGUID(path);

				if (!assetsToReference.ContainsKey(guid))
					assetsToReference.Add(guid, new AssetInfo { GUID = guid, Type = pt, Path = path });
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
		}
	}

	static void CreateReferenceFile(AssetInfo info)
	{
		if (string.IsNullOrEmpty(info.GUID))
			return;

		string src = info.Path;
		string dest = string.Format("Assets/Resources/_GeneratedWeaks_/{0}_{1}.asset", info.GUID, FilterPath(src));

		var obj = AssetDatabase.LoadAssetAtPath(src, info.Type);

		var so = ScriptableObject.CreateInstance<UnityWeakReferenceScriptableObject>();
		so.UnityObject = obj;

		Debug.LogFormat("[UnityWeakReference] Created {0} -> {1}", src, dest);

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
