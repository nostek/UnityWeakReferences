public interface IUnityWeakReferenceCallbacks
{
	//Gets guid of prefab and clears prefab reference.
	//This is called before it clears the prefab.
	void OnWeakUnityReferenceGenerate();

	//Restores changed prefab value.
	//This is called after the restoration is complete.
	void OnWeakUnityReferenceRestore();

	//Called from OnPostProcessScene callback.
	//Can change scene object without changing the saved file.
	void OnWeakUnityReferenceProcessScene(bool isPlaying);
}
