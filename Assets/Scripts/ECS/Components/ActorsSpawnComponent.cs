using Unity.Entities;
using Unity.Mathematics;

public struct ActorsSpawnComponent : IComponentData
{
    //-----------PIGS
    public Entity PigPrefab;
    public int PigsCount;
    public float SpawnHeightMin;
    public float SpawnHeightMax;
    public float PigSpeed;

    //-----------GRASS
    public Entity GrassPrefab;
    public int GrassCount;
    public float GrassRadiusMax;
    public float GrassWholenessMin; 
    public float GrassWholenessMax;
    public float GrassNutritionMin;
    public float GrassNutritionMax;

    //-----------TEST
    public Entity IcoTest;
}
