using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Game.Common.Components
{
    //[GhostComponentVariation(typeof(KinematicCharacterBody))]
    //[GhostComponent]
    //public struct KinematicCharacterBodyGhostVariant
    //{
    //    [GhostField] public Entity ParentEntity;

    //    [GhostField] public bool IsGrounded;

    //    [GhostField(Quantization = 1000)] public float3 RelativeVelocity;
    //    [GhostField(Quantization = 1000)] public float3 ParentLocalAchorPoint;
    //    [GhostField(Quantization = 1000)] public float3 ParentVelocity;       
    //}

    //[GhostComponentVariation(typeof(CharacterInterpolation))]
    //[GhostComponent(PrefabType = GhostPrefabType.PredictedClient)]
    //public struct CharacterInterpolationGhostVariant
    //{

    //}
}