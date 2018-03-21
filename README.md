# UnityWeakReferences
Hard references to prefabs are converted to weak references at build time.

## WORK IN PROGRESS

### Supported types
- Prefab/GameObject
- Sprite

### Known issues
- If used in a script in the project view, the referenced object will be added to the build even if the reference holder is not.
