using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine.SceneManagement;

public sealed class EditorUnityWeakReference : IPreprocessBuild, IProcessScene, IPostprocessBuild
{
	const int MaxDepth = 5;

	enum DoWorkState
	{
		OnPreprocessBuild,
		OnProcessScene,
		OnPostprocessBuild,
		DoNothing
	}

	public int callbackOrder { get { return 0; } }

	public void OnPreprocessBuild(BuildTarget target, string pathToBuild)
	{
		PrepareFolder();

		List<string> guidsToCopy = new List<string>();

		var guids = AssetDatabase.FindAssets("t:Prefab");
		foreach (var g in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(g);
			var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);

			DoWork(go, DoWorkState.OnPreprocessBuild, false, guidsToCopy);
		}

		guids = AssetDatabase.FindAssets("t:ScriptableObject");
		foreach (var g in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(g);
			var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

			DoWork(so, DoWorkState.OnPreprocessBuild, false, guidsToCopy);
		}

		AssetDatabase.SaveAssets();

		for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCountInBuildSettings; i++)
		{
			var scenePath = SceneUtility.GetScenePathByBuildIndex(i);

			var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

			var objs = scene.GetRootGameObjects();
			foreach (var go in objs)
				DoWork(go, DoWorkState.OnPreprocessBuild, true, guidsToCopy);
		}

		foreach (var g in guidsToCopy)
			CopyFile(g);
	}

	public void OnProcessScene(Scene scene)
	{
		var objs = scene.GetRootGameObjects();
		foreach (var go in objs)
			DoWork(go, DoWorkState.OnProcessScene, true, null);
	}

	public void OnPostprocessBuild(BuildTarget target, string pathToBuild)
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

	static void DoWork(GameObject go, DoWorkState state, bool sceneObjects, List<string> guidsToCopy)
	{
		var behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);

		foreach (var b in behaviours)
			DoWork(b, state, sceneObjects, guidsToCopy);
	}

	static void DoWork(UnityEngine.Object obj, DoWorkState state, bool sceneObjects, List<string> guidsToCopy)
	{
		bool haveWeakUnityReference = ProcessWork(obj, DoWorkState.DoNothing, sceneObjects, guidsToCopy, 0);

		if (state == DoWorkState.OnPreprocessBuild && haveWeakUnityReference && obj is IUnityWeakReferenceCallbacks)
			(obj as IUnityWeakReferenceCallbacks).OnWeakUnityReferenceGenerate();

		if (state == DoWorkState.OnProcessScene && haveWeakUnityReference && obj is IUnityWeakReferenceCallbacks)
			(obj as IUnityWeakReferenceCallbacks).OnWeakUnityReferenceProcessScene(Application.isPlaying);

		bool changed = ProcessWork(obj, state, sceneObjects, guidsToCopy, 0);

		if (changed)
			UnityEditor.EditorUtility.SetDirty(obj);

		if (state == DoWorkState.OnPostprocessBuild && haveWeakUnityReference && obj is IUnityWeakReferenceCallbacks)
			(obj as IUnityWeakReferenceCallbacks).OnWeakUnityReferenceRestore();
	}

	static bool ProcessWork(System.Object obj, DoWorkState state, bool sceneObjects, List<string> guidsToCopy, int depth)
	{
		if (depth > MaxDepth)
			return false;

		bool changed = false;

		if (obj is UnityWeakReference)
		{
			changed = ProcessWeakReference(obj as UnityWeakReference, state, sceneObjects, guidsToCopy) || changed;

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

					changed = ProcessWork(ao, state, sceneObjects, guidsToCopy, depth + 1) || changed;
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

				changed = ProcessWork(o, state, sceneObjects, guidsToCopy, depth + 1) || changed;

				continue;
			}
		}

		return changed;
	}

	static bool ProcessWeakReference(UnityWeakReference p, DoWorkState state, bool sceneObjects, List<string> guidsToCopy)
	{
		bool changed = false;

		var pf = p.GetType().BaseType.GetField("reference", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var pg = p.GetType().BaseType.GetField("assetGuid", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var pp = p.GetType().BaseType.GetField("path", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var pt = (p.GetType().GetCustomAttributes(typeof(UnityWeakReferenceType), false)[0] as UnityWeakReferenceType).Type;

		if (state == DoWorkState.DoNothing)
			changed = true;

		if (state == DoWorkState.OnPreprocessBuild)
		{
			UnityEngine.Object pgo = pf.GetValue(p) as UnityEngine.Object;

			if (pgo != null)
			{
				var path = AssetDatabase.GetAssetPath(pgo);
				var guid = AssetDatabase.AssetPathToGUID(path);

				if (!guidsToCopy.Contains(guid))
					guidsToCopy.Add(guid);

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

	static void CopyFile(string guid)
	{
		if (!string.IsNullOrEmpty(guid))
		{
			string src = AssetDatabase.GUIDToAssetPath(guid);

			if (src.StartsWith("Assets/Resources/"))
			{
				Debug.LogFormat("[UnityWeakReference] Do not need to copy: {0}", src);
				return;
			}

			string ext = src.Substring(src.LastIndexOf("."));
			string dest = "Assets/Resources/_GeneratedWeaks_/" + guid + "_" + FilterPath(src) + ext;

			Debug.LogFormat("[UnityWeakReference] Copy {0} -> {1}", src, dest);

			AssetDatabase.CopyAsset(src, dest);
			AssetDatabase.ImportAsset(dest, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
		}
	}

	static string FilePath(string guid)
	{
		if (!string.IsNullOrEmpty(guid))
		{
			string src = AssetDatabase.GUIDToAssetPath(guid);

			if (src.StartsWith("Assets/Resources/"))
			{
				string path = src.Substring(("Assets/Resources/").Length);
				path = path.Substring(0, path.LastIndexOf("."));

				Debug.LogFormat("[UnityWeakReference] Will use path: {0}", path);

				return path;
			}

			return "_GeneratedWeaks_/" + guid + "_" + FilterPath(src);
		}
		return null;
	}

	static string FilterPath(string path)
	{
		path = path.Substring(0, path.LastIndexOf("."));
		path = path.Replace('.', '_');
		path = path.Replace('/', '_');
		return path;
	}
}
