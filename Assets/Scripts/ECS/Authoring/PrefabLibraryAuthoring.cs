using Unity.Entities;
using UnityEngine;

public class PrefabLibraryAuthoring : MonoBehaviour
{
    public GameObject Grass;
    public GameObject Pig;
    public GameObject Wolf;

    public class Baker : Baker<PrefabLibraryAuthoring>
    {
        public override void Bake(PrefabLibraryAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new PrefabsLibraryComponent
            {
                Pig = GetEntity(authoring.Pig, TransformUsageFlags.Dynamic),
                Grass = GetEntity(authoring.Grass, TransformUsageFlags.Dynamic),
                Wolf = GetEntity(authoring.Wolf, TransformUsageFlags.Dynamic)
            });

            Debug.Log("Prefab library BLOB is created");
        }
    }
}
