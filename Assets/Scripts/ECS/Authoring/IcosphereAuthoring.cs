using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// Authoring component that takes a Planet mono object
public class IcosphereAuthoring : MonoBehaviour
{
    public int TesselationLevel = 1;

    public class Baker : Baker<IcosphereAuthoring>
    {
        public override void Bake(IcosphereAuthoring authoring)
        {
            // Create the blob asset
            BlobAssetReference<IcosphereMapBlob> blobAsset = CreateIcosphereBlob((authoring.transform).localScale.x / 2f, authoring.TesselationLevel);

            var entity = GetEntity(TransformUsageFlags.None);

            // Add the component to the entity
            AddComponent(entity, new IcosphereComponent
            {
                BlobRef = blobAsset
            });
        }

        private BlobAssetReference<IcosphereMapBlob> CreateIcosphereBlob(float radius, int tesselationLevel)
        {
            // Builder for creating the blob
            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref IcosphereMapBlob blob = ref builder.ConstructRoot<IcosphereMapBlob>();

            NativeList<Triangle> triangles = new NativeList<Triangle>(Allocator.Temp);
            CreateInitialIcosahedron(ref triangles, radius);
            //CreateInitialThetrahedron(ref triangles, radius);

            NativeList<Triangle> tesellatedTriangles = new NativeList<Triangle>(Allocator.Temp);

            // Apply tesselation based on the desired level
            for (int i = 1; i < tesselationLevel; i++)
            {
                tesellatedTriangles.Clear();
                TesselateTriangles(ref tesellatedTriangles, triangles, radius);
                triangles.Clear();
                foreach (var triangle in tesellatedTriangles)
                {
                    triangles.Add(triangle);
                }
            }

            tesellatedTriangles.Dispose();

            // Allocate the triangles array in the blob
            BlobBuilderArray<Triangle> blobTriangles = builder.Allocate(ref blob.Triangles, triangles.Length);

            // Set up the neighboring triangles
            BlobBuilderArray<int> neighbors = builder.Allocate(ref blob.NeighbouringTriangles, 3 * triangles.Length);

            // Calculate the minimum and maximum radii across all triangles
            float minInnerRadius = float.MaxValue;
            float maxOuterRadius = 0f;
            var neighbourIndex = 0;

            // Fill the triangles data
            for (int i = 0; i < triangles.Length; i++)
            {
                var triangle = triangles[i];
                triangle.TriangleIndex = i;

                // Calculate the centroid
                triangle.Centroid = (triangle.V1 + triangle.V2 + triangle.V3) / 3f;

                // Project the centroid onto the sphere surface
                triangle.CentroidOnSurface = math.normalize(triangle.Centroid) * radius;

                // Calculate inner and outer radii for the triangle
                float innerRadius = CalculateInnerRadius(triangle.V1, triangle.V2, triangle.V3);
                float outerRadius = CalculateOuterRadius(triangle.V1, triangle.V2, triangle.V3);

                triangle.RadiusInner = innerRadius;
                triangle.RadiusOuter = outerRadius;

                // Update global min/max
                minInnerRadius = math.min(minInnerRadius, innerRadius);
                maxOuterRadius = math.max(maxOuterRadius, outerRadius);

                blobTriangles[i] = triangle;

                for (int j = 0; j < 3; j++)
                {
                    // In a real implementation, we'd calculate the actual neighbors
                    // This is a placeholder that would be replaced with proper neighbor calculation
                    neighbors[neighbourIndex] = FindNeighborIndex(ref triangles, triangle, j);
                    neighbourIndex++;
                }
            }

            // Set the global min/max radii
            blob.RadiusInner = minInnerRadius;
            blob.RadiusOuter = maxOuterRadius;

            // Create the final blob asset
            BlobAssetReference<IcosphereMapBlob> blobAsset = builder.CreateBlobAssetReference<IcosphereMapBlob>(Allocator.Persistent);
            builder.Dispose();
            triangles.Dispose();

            return blobAsset;
        }
        private void CreateInitialIcosahedron(ref NativeList<Triangle> output, float radius)
        {
            // Golden ratio for icosahedron construction
            float t = (1f + math.sqrt(5f)) / 2f;

            var vertices = new NativeList<float3>(12, Allocator.Temp)
            {
                math.normalize(new float3(-1f, t, 0f)) * radius,
                math.normalize(new float3(1f, t, 0f)) * radius,
                math.normalize(new float3(-1f, -t, 0f)) * radius,
                math.normalize(new float3(1f, -t, 0f)) * radius,

                math.normalize(new float3(0f, -1f, t)) * radius,
                math.normalize(new float3(0f, 1f, t)) * radius,
                math.normalize(new float3(0f, -1f, -t)) * radius,
                math.normalize(new float3(0f, 1f, -t)) * radius,

                math.normalize(new float3(t, 0f, -1f)) * radius,
                math.normalize(new float3(t, 0f, 1f)) * radius,
                math.normalize(new float3(-t, 0f, -1f)) * radius,
                math.normalize(new float3(-t, 0f, 1f)) * radius
            };

            // Create the triangle list
            int[,] triangleIndices = new int[,]
            {
                {0, 11, 5}, {0, 5, 1}, {0, 1, 7}, {0, 7, 10}, {0, 10, 11},
                {1, 5, 9}, {5, 11, 4}, {11, 10, 2}, {10, 7, 6}, {7, 1, 8},
                {3, 9, 4}, {3, 4, 2}, {3, 2, 6}, {3, 6, 8}, {3, 8, 9},
                {4, 9, 5}, {2, 4, 11}, {6, 2, 10}, {8, 6, 7}, {9, 8, 1}
            };

            for (int i = 0; i < triangleIndices.GetLength(0); i++)
            {
                Triangle tri = new Triangle
                {
                    V1 = vertices[triangleIndices[i, 0]],
                    V2 = vertices[triangleIndices[i, 1]],
                    V3 = vertices[triangleIndices[i, 2]],
                    TriangleIndex = i
                };
                output.Add(tri);
            }

            vertices.Dispose();
        }

        private void CreateInitialThetrahedron(ref NativeList<Triangle> output, float radius)
        {
            var vertices = new NativeList<float3>(4, Allocator.Temp);

            vertices.Add(new float3(0f, radius, 0f));

            // Base vertices
            vertices.Add(new float3((2f * math.sqrt(2f) / 3f) * radius, -radius / 3f, 0f));
            vertices.Add(new float3((-math.sqrt(2f) / 3f) * radius, -radius / 3f, (math.sqrt(6f) / 3f) * radius));
            vertices.Add(new float3((-math.sqrt(2f) / 3f) * radius, -radius / 3f, (-math.sqrt(6f) / 3f) * radius));

            int[,] triangleIndices = new int[4, 3]
            {
                    // Face 1: Apex, Vertex 2, Vertex 3
                    { 0, 1, 2 },
                    // Face 2: Apex, Vertex 2, Vertex 4
                    { 0, 1, 3 },
                    // Face 3: Apex, Vertex 3, Vertex 4
                    { 0, 2, 3 },
                    // Face 4: Vertex 2, Vertex 3, Vertex 4
                    { 1, 2, 3 }
            };

            for (int i = 0; i < triangleIndices.GetLength(0); i++)
            {
                Triangle tri = new Triangle
                {
                    V1 = vertices[triangleIndices[i, 0]],
                    V2 = vertices[triangleIndices[i, 1]],
                    V3 = vertices[triangleIndices[i, 2]],
                    TriangleIndex = i
                };
                output.Add(tri);
            }

            vertices.Dispose();

        }

        private void TesselateTriangles(ref NativeList<Triangle> tesellatedOutput, NativeList<Triangle> triangles, float radius)
        {
            foreach (Triangle tri in triangles)
            {
                // Calculate midpoints on each edge, then project to sphere
                float3 v1 = tri.V1;
                float3 v2 = tri.V2;
                float3 v3 = tri.V3;

                float3 v12 = math.normalize((v1 + v2) / 2f) * radius;
                float3 v23 = math.normalize((v2 + v3) / 2f) * radius;
                float3 v31 = math.normalize((v3 + v1) / 2f) * radius;

                // Create 4 new triangles
                tesellatedOutput.Add(new Triangle { V1 = v1, V2 = v12, V3 = v31 });
                tesellatedOutput.Add(new Triangle { V1 = v12, V2 = v2, V3 = v23 });
                tesellatedOutput.Add(new Triangle { V1 = v31, V2 = v12, V3 = v23 });
                tesellatedOutput.Add(new Triangle { V1 = v31, V2 = v23, V3 = v3 });
            }
        }

        private float CalculateInnerRadius(float3 v1, float3 v2, float3 v3)
        {
            // Calculate the radius of the inscribed circle
            float a = math.length(v2 - v3);
            float b = math.length(v1 - v3);
            float c = math.length(v1 - v2);

            float s = (a + b + c) / 2f; // Semi-perimeter
            float area = math.sqrt(s * (s - a) * (s - b) * (s - c));

            return 2f * area / (a + b + c);
        }

        private float CalculateOuterRadius(float3 v1, float3 v2, float3 v3)
        {
            // Calculate the radius of the circumscribed circle
            var mid = (v1 + v2 + v3) / 3f;

            return math.length(v1 - mid);
        }

        private int FindNeighborIndex(ref NativeList<Triangle> triangles, Triangle triangle, int edgeIndex)
        {
            // Get the two vertices of the edge
            float3 v1, v2;
            switch (edgeIndex)
            {
                case 0: v1 = triangle.V1; v2 = triangle.V2; break;
                case 1: v1 = triangle.V2; v2 = triangle.V3; break;
                case 2: v1 = triangle.V3; v2 = triangle.V1; break;
                default: throw new ArgumentOutOfRangeException();
            }

            // Find a triangle that shares these two vertices but is not the current triangle
            for (int i = 0; i < triangles.Length; i++)
            {
                if (i == triangle.TriangleIndex) continue;

                Triangle candidate = triangles[i];
                int sharedVertices = 0;

                // Check if v1 is in the candidate triangle
                if (math.all(math.abs(candidate.V1 - v1) < 0.0001f) ||
                    math.all(math.abs(candidate.V2 - v1) < 0.0001f) ||
                    math.all(math.abs(candidate.V3 - v1) < 0.0001f))
                {
                    sharedVertices++;
                }

                // Check if v2 is in the candidate triangle
                if (math.all(math.abs(candidate.V1 - v2) < 0.0001f) ||
                    math.all(math.abs(candidate.V2 - v2) < 0.0001f) ||
                    math.all(math.abs(candidate.V3 - v2) < 0.0001f))
                {
                    sharedVertices++;
                }

                if (sharedVertices == 2)
                {
                    return i;
                }
            }

            // If no neighbor found, return -1
            return -1;
        }
    }

}

// The blob data structure
public struct Triangle
{
    public float3 V1;
    public float3 V2;
    public float3 V3;
    public float3 Centroid;
    public float3 CentroidOnSurface; // Centroid position on the surface of the sphere
    public int TriangleIndex; // Index of the triangle in the array in blob
    public float RadiusInner; // radius of the circle inscribed in triangle
    public float RadiusOuter; // radius of the circle outside of triangle
}

public struct IcosphereMapBlob
{
    public BlobArray<Triangle> Triangles;
    public BlobArray<int> NeighbouringTriangles;
    public float RadiusInner; // minimum inner radius across all triangles
    public float RadiusOuter; // maximum outer radius across all triangles
}

// Component that references the blob
public struct IcosphereComponent : IComponentData
{
    public BlobAssetReference<IcosphereMapBlob> BlobRef;
}

// Extension methods for IcosphereMapBlob
public static partial class IcosphereMapExtensions
{
    public static int Length(this in IcosphereComponent blob)
    {
        return blob.BlobRef.Value.Triangles.Length;
    }

    public static Triangle GetTriangle(this in IcosphereComponent blob, int index)
    {
        return blob.BlobRef.Value.Triangles[index];
    }

    public static int GetTriangleIndex(this in IcosphereComponent blob, float3 vector)
    {
        // Normalize the input vector to ensure it's on the sphere surface
        float3 normalizedVector = math.normalize(vector);

        // Find the triangle with the closest centroid
        float minDistance = float.MaxValue;
        int closestTriangleIndex = -1;

        for (int i = 0; i < blob.BlobRef.Value.Triangles.Length; i++)
        {
            float distance = math.distance(normalizedVector, math.normalize(blob.BlobRef.Value.Triangles[i].CentroidOnSurface));
            if (distance < minDistance)
            {
                minDistance = distance;
                closestTriangleIndex = i;
            }
        }

        return closestTriangleIndex;
    }

    public static Triangle GetTriangle(this in IcosphereComponent blob, float3 vector)
    {
        int index = blob.GetTriangleIndex(vector);
        return blob.GetTriangle(index);
    }

    public static int GetNeighbouringTriangleIndex(this in IcosphereComponent blob, int triangleIndex, int neighbourIndex)
    {
        var globalIndex = neighbourIndex + triangleIndex * 3;
        return blob.BlobRef.Value.NeighbouringTriangles[globalIndex];
    }

    public static quaternion GetRotation(this in IcosphereComponent blobRef, int triangleIndex, int forwardVertexIndex = 0)
{
        ref var triangle = ref blobRef.BlobRef.Value.Triangles[triangleIndex];

        // Get the forward direction (from centroid to the specified vertex)
        float3 forward;
        switch (forwardVertexIndex)
        {
            case 0: forward = triangle.V1 - triangle.Centroid; break;
            case 1: forward = triangle.V2 - triangle.Centroid; break;
            case 2: forward = triangle.V3 - triangle.Centroid; break;
            default: throw new ArgumentOutOfRangeException();
        }

        // Calculate the up direction (from centroid to surface)
        float3 up = triangle.Centroid;

        // Create a rotation that aligns with these directions
        return quaternion.LookRotation(math.normalize(forward), math.normalize(up));
    }
    /*
    public partial struct TestIcoSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<IcosphereComponent>();
            state.RequireForUpdate<ActorsSpawnComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            var ico = SystemAPI.GetSingleton<IcosphereComponent>();
            var commandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            var entity = SystemAPI.GetSingletonEntity<ActorsSpawnComponent>();
            var spawnData = SystemAPI.GetComponentRO<ActorsSpawnComponent>(entity);

            for (int i = 0; i < ico.Length(); i++)
            {
                var triangle = ico.GetTriangle(i);
                var scale = 1;

                var newGrass1 = commandBuffer.Instantiate(spawnData.ValueRO.IcoTest);
                commandBuffer.SetComponent(newGrass1, new LocalTransform { Position = triangle.V1, Rotation = quaternion.identity, Scale = scale });
                var newGrass2 = commandBuffer.Instantiate(spawnData.ValueRO.IcoTest);
                commandBuffer.SetComponent(newGrass2, new LocalTransform { Position = triangle.V2, Rotation = quaternion.identity, Scale = scale });
                var newGrass3 = commandBuffer.Instantiate(spawnData.ValueRO.IcoTest);
                commandBuffer.SetComponent(newGrass3, new LocalTransform { Position = triangle.V3, Rotation = quaternion.identity, Scale = scale });
 
                var pos = triangle.CentroidOnSurface;
                var rotation = ico.GetRotation(i);
                var scaleGrass = triangle.RadiusOuter * 2;
                var newGrass = commandBuffer.Instantiate(spawnData.ValueRO.GrassPrefab);
                commandBuffer.SetComponent(newGrass, new LocalTransform { Position = pos, Rotation = rotation, Scale = scaleGrass });

            }


            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }*/
}