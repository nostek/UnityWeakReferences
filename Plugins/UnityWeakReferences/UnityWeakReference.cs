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

	public abstract System.Type GetWeakType();

	protected void SetObject(UnityEngine.Object reference)
	{
		this.reference = reference;
	}

	protected T_Class Get<T_Class>()
		where T_Class : UnityEngine.Object
	{
		if (reference == null && path != null)
			reference = Resources.Load<T_Class>(path);

		return (T_Class)reference;
	}
}

[System.Serializable]
public sealed class WeakPrefab : UnityWeakReference
{
	public override System.Type GetWeakType()
	{
		return typeof(GameObject);
	}

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
public sealed class WeakSprite : UnityWeakReference
{
	public override System.Type GetWeakType()
	{
		return typeof(Sprite);
	}

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
public sealed class WeakTexture2D : UnityWeakReference
{
	public override System.Type GetWeakType()
	{
		return typeof(Texture2D);
	}

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
