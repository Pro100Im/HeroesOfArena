using Unity.Entities;
using Random = Unity.Mathematics.Random;

//namespace Unity.Template.CompetitiveActionMultiplayer
//{
    public struct CameraTarget : IComponentData
    {
        //public Entity TargetEntity;

        public float BaseFov;
    }

    public struct FixedRandom : IComponentData
    {
        public Random Random;
    }
//}
