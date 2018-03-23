# UnityWeakReferences
Hard references to prefabs are converted to weak references at build time.

## WORK IN PROGRESS

### Installation
Copy the UnityWeakReference folder in to your project. Use WeakPrefab/WeakSprite/WeakTexture2D/WeakAudioClip in your MonoBehaviour/Array/List/Custom Class and fill out the field in the editor. All set!

### Example
> public WeakPrefab MyPrefab;
>
> void Start()
> {
>  Instantiate(MyPrefab.Prefab);
> }

### Supported types
- Prefab/GameObject
- Sprite
- Texture2D
- AudioClip

### Known issues
- If used in a script in the project view, the referenced object will be added to the build even if the reference holder is not.
- To speed up the processing it looks _MaxDepth_ layers down. The lower the faster, but deeper nesting needs higher. __Default__ is 5.
