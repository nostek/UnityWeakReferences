using UnityEngine;

//Class need to be NOT abstract, otherwise the player will throw exceptions on #ifdef'ing out variables
[System.Serializable]
public class UnityWeakReference : ISerializationCallbackReceiver
{
#if UNITY_EDITOR
	[SerializeField]
	UnityEngine.Object reference = null;
#endif

	[SerializeField]
	string path = null;

#pragma warning disable 0414
	[SerializeField]
	string assetGuid = null;

	public void OnAfterDeserialize()
	{
	}

	public void OnBeforeSerialize()
	{
#if UNITY_EDITOR
		if (reference == null)
		{
			assetGuid = "";
			path = "";
		}
		else
		{
			if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating)
				return;

			var p = UnityEditor.AssetDatabase.GetAssetPath(reference);
			var g = UnityEditor.AssetDatabase.AssetPathToGUID(p);

			assetGuid = g;

			p = p.Substring(0, p.LastIndexOf("."));
			p = p.Replace('.', '_');
			p = p.Replace('/', '_');
			p = p.Replace(' ', '_');
			path = string.Format("_GeneratedWeaks_/{0}/{1}_{2}", g[0], g, p);
		}
#endif
	}
#pragma warning restore

#if UNITY_EDITOR
	public void SetObject(UnityEngine.Object reference)
	{
		this.reference = reference;
	}
#endif

	protected T_Class Get<T_Class>()
		where T_Class : UnityEngine.Object
	{
#if UNITY_EDITOR
		if (reference != null)
			return (T_Class)reference;
#else
		if (!string.IsNullOrEmpty(path))
		{
			var so = Resources.Load<UnityWeakReferenceScriptableObject>(path);
			UnityEngine.Assertions.Assert.IsNotNull(so, $"Could not load Weak SO {path}");
			return (T_Class)so.UnityObject;
		}
#endif

		return default(T_Class);
	}
}

[System.Serializable]
[UnityWeakReferenceType(typeof(GameObject))]
public sealed class WeakPrefab : UnityWeakReference
{
	public GameObject Prefab
	{
		get
		{
			return Get<GameObject>();
		}
	}
}

[System.Serializable]
[UnityWeakReferenceType(typeof(Sprite))]
public sealed class WeakSprite : UnityWeakReference
{
	public Sprite Sprite
	{
		get
		{
			return Get<Sprite>();
		}
	}
}

[System.Serializable]
[UnityWeakReferenceType(typeof(Texture2D))]
public sealed class WeakTexture2D : UnityWeakReference
{
	public Texture2D Texture
	{
		get
		{
			return Get<Texture2D>();
		}
	}
}

[System.Serializable]
[UnityWeakReferenceType(typeof(AudioClip))]
public sealed class WeakAudioClip : UnityWeakReference
{
	public AudioClip AudioClip
	{
		get
		{
			return Get<AudioClip>();
		}
	}
}
