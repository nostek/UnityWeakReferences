using UnityEngine;

[System.Serializable]
public abstract class UnityWeakReference
{
	[SerializeField]
	UnityEngine.Object reference = null;

	[SerializeField]
	string path = null;

#pragma warning disable 0414
	[SerializeField]
	string assetGuid = null;
#pragma warning restore

	protected void SetObject(UnityEngine.Object reference)
	{
		this.reference = reference;
	}

	protected T_Class Get<T_Class>()
		where T_Class : UnityEngine.Object
	{
		if (reference != null)
			return (T_Class)reference;

		if (path != null)
			return Resources.Load<T_Class>(path);

		return null;
	}
}

[System.Serializable]
[UnityWeakReferenceType(typeof(GameObject))]
public sealed class WeakPrefab : UnityWeakReference
{
	public GameObject Prefab
	{
		set
		{
			SetObject(value);
		}
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
		set
		{
			SetObject(value);
		}
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
		set
		{
			SetObject(value);
		}
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
		set
		{
			SetObject(value);
		}
		get
		{
			return Get<AudioClip>();
		}
	}
}
