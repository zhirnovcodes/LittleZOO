using Unity.Entities;
using Unity.Mathematics;

namespace Zoo.Physics
{
    public struct GravityComponent : IComponentData
    {
        public float3 GravityVelocity;
        public float3 GravityDirection;
        public bool IsTouchingPlanet;
    }
}
