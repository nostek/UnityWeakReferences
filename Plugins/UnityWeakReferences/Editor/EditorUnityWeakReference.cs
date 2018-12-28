using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine.SceneManagement;
using UnityEditor.Build.Reporting;

public sealed class EditorUnityWeakReference : IPreprocessBuildWithReport, IProcessSceneWithReport, IPostprocessBuildWithReport
{
	const int MaxDepth = 5;

	enum DoWorkState
	{
		Invalid = 0,
		OnPreprocessBuild,
		OnProcessScene,
		OnPostprocessBuild,
	}

	struct AssetInfo
	{
		public string GUID;
		public System.Type Type;
	}

	public int callbackOrder { get { return 0; } }

	public void OnPreprocessBuild(BuildReport report)
	{
		PrepareFolder();

		Dictionary<string, AssetInfo> assetsToReference = new Dictionary<string, AssetInfo>();

		var guids = AssetDatabase.FindAssets("t:Prefab");
		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);

			bool changed = DoWork(go, DoWorkState.OnPreprocessBuild, false, assetsToReference);
		}

		AssetDatabase.SaveAssets();

		guids = AssetDatabase.FindAssets("t:ScriptableObject");
		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

			bool changed = DoWork(so, DoWorkState.OnPreprocessBuild, false, assetsToReference);
		}

		AssetDatabase.SaveAssets();

		for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCountInBuildSettings; i++)
		{
			var scenePath = SceneUtility.GetScenePathByBuildIndex(i);

			if (string.IsNullOrEmpty(scenePath))
				continue;

			var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

			var objs = scene.GetRootGameObjects();
			foreach (var go in objs)
				DoWork(go, DoWorkState.OnPreprocessBuild, true, assetsToReference);
		}

		foreach (var a in assetsToReference)
			CreateReferenceFile(a.Value);

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
	}

	public void OnProcessScene(Scene scene, BuildReport report)
	{
		var objs = scene.GetRootGameObjects();
		foreach (var go in objs)
			DoWork(go, DoWorkState.OnProcessScene, true, null);
	}

	public void OnPostprocessBuild(BuildReport report)
	{
		var guids = AssetDatabase.FindAssets("t:Prefab");
		foreach (var g in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(g);
			var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);

			DoWork(go, DoWorkState.OnPostprocessBuild, false, null);
		}

		guids = AssetDatabase.FindAssets("t:ScriptableObject");
		foreach (var g in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(g);
			var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

			DoWork(so, DoWorkState.OnPostprocessBuild, false, null);
		}

		AssetDatabase.SaveAssets();

		for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCountInBuildSettings; i++)
		{
			var scenePath = SceneUtility.GetScenePathByBuildIndex(i);

			var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

			var objs = scene.GetRootGameObjects();
			foreach (var go in objs)
				DoWork(go, DoWorkState.OnPostprocessBuild, true, null);
		}

		PrepareFolder(false);
	}

	/////////////////////

	static bool DoWork(GameObject go, DoWorkState state, bool sceneObjects, Dictionary<string, AssetInfo> assetsToReference)
	{
		if (go == null)
			return false;

		var behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);

		bool changed = false;

		foreach (var b in behaviours)
			changed = DoWork(b, state, sceneObjects, assetsToReference) || changed;

		if (changed)
			UnityEditor.EditorUtility.SetDirty(go);

		return changed;
	}

	static bool DoWork(UnityEngine.Object obj, DoWorkState state, bool sceneObjects, Dictionary<string, AssetInfo> assetsToReference)
	{
		if (obj == null)
			return false;

		bool changed = ProcessWork(obj, state, sceneObjects, assetsToReference, 0);

		if (changed)
			UnityEditor.EditorUtility.SetDirty(obj);

		return changed;
	}

	static bool ProcessWork(System.Object obj, DoWorkState state, bool sceneObjects, Dictionary<string, AssetInfo> assetsToReference, int depth)
	{
		if (depth > MaxDepth)
			return false;

		bool changed = false;

		if (obj is UnityWeakReference)
		{
			changed = ProcessWeakReference(obj as UnityWeakReference, state, sceneObjects, assetsToReference) || changed;

			return changed;
		}

		var fields = obj.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		foreach (var f in fields)
		{
			if (f.FieldType.IsArray)
			{
				var o = f.GetValue(obj);

				if (o == null)
					continue;

				System.Array a = (System.Array)o;

				for (int ai = 0; ai < a.Length; ai++)
				{
					var ao = a.GetValue(ai);

					if (ao == null)
						continue;

					if (!ao.GetType().IsSerializable)
						continue;

					if (ao.GetType().IsPrimitive)
						continue;

					if (ao.GetType().IsValueType)
						continue;

					changed = ProcessWork(ao, state, sceneObjects, assetsToReference, depth + 1) || changed;
				}

				continue;
			}

			if (f.FieldType.IsClass)
			{
				var o = f.GetValue(obj);

				if (o == null)
					continue;

				if (!o.GetType().IsSerializable)
					continue;

				if (o.GetType().IsPrimitive)
					continue;

				if (o.GetType().IsValueType)
					continue;

				changed = ProcessWork(o, state, sceneObjects, assetsToReference, depth + 1) || changed;

				continue;
			}
		}

		return changed;
	}

	static bool ProcessWeakReference(UnityWeakReference p, DoWorkState state, bool sceneObjects, Dictionary<string, AssetInfo> assetsToReference)
	{
		bool changed = false;

		var pf = p.GetType().BaseType.GetField("reference", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var pg = p.GetType().BaseType.GetField("assetGuid", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var pp = p.GetType().BaseType.GetField("path", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var pt = (p.GetType().GetCustomAttributes(typeof(UnityWeakReferenceType), false)[0] as UnityWeakReferenceType).Type;

		if (state == DoWorkState.OnPreprocessBuild)
		{
			UnityEngine.Object pgo = pf.GetValue(p) as UnityEngine.Object;

			if (pgo != null)
			{
				var path = AssetDatabase.GetAssetPath(pgo);
				var guid = AssetDatabase.AssetPathToGUID(path);

				if (!assetsToReference.ContainsKey(guid))
					assetsToReference.Add(guid, new AssetInfo { GUID = guid, Type = pt });

				if (!sceneObjects)
				{
					pp.SetValue(p, FilePath(guid));
					pg.SetValue(p, guid);
					pf.SetValue(p, null);
				}

				changed = true;
			}
		}

		if (state == DoWorkState.OnProcessScene && sceneObjects && !Application.isPlaying)
		{
			UnityEngine.Object pgo = pf.GetValue(p) as UnityEngine.Object;

			if (pgo != null)
			{
				var path = AssetDatabase.GetAssetPath(pgo);
				var guid = AssetDatabase.AssetPathToGUID(path);

				pp.SetValue(p, FilePath(guid));
				pg.SetValue(p, guid);
				pf.SetValue(p, null);
			}
		}

		if (state == DoWorkState.OnPostprocessBuild)
		{
			string guid = pg.GetValue(p) as string;

			if (guid != null)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var prefab = AssetDatabase.LoadAssetAtPath(path, pt);

				if (!sceneObjects)
				{
					pf.SetValue(p, prefab);
					pp.SetValue(p, null);
					pg.SetValue(p, null);
				}

				changed = true;
			}
		}

		return changed;
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

		string src = AssetDatabase.GUIDToAssetPath(info.GUID);
		string dest = "Assets/Resources/_GeneratedWeaks_/" + info.GUID + "_" + FilterPath(src) + ".asset";

		var obj = AssetDatabase.LoadAssetAtPath(src, info.Type);

		var so = ScriptableObject.CreateInstance<UnityWeakReferenceScriptableObject>();
		so.UnityObject = obj;

		Debug.LogFormat("[UnityWeakReference] Created {0} -> {1}", src, dest);

		AssetDatabase.CreateAsset(so, dest);
	}

	static string FilePath(string guid)
	{
		if (!string.IsNullOrEmpty(guid))
		{
			string src = AssetDatabase.GUIDToAssetPath(guid);

			return "_GeneratedWeaks_/" + guid + "_" + FilterPath(src);
		}
		return null;
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
