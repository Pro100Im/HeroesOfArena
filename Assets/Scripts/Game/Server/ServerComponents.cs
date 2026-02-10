using Unity.Entities;
using Unity.NetCode;

namespace Game.Server
{
    public struct GameplayMaps : IBufferElementData
    {
        /// <summary>The <see cref="NetworkStreamConnection"/> entity for this <see cref="NetworkId"/> index.</summary>
        public Entity ConnectionEntity;
        /// <summary>The <see cref="FirstPersonPlayer"/> entity for this <see cref="NetworkId"/> index.</summary>
        public Entity FirstPersonPlayersEntity;
        /// <summary>The <see cref="FirstPersonCharacterControl"/> entity for this <see cref="NetworkId"/> index.</summary>
        public Entity CharacterControllerEntity;

        /// <summary>If != default, need to remap this to the entity.</summary>
        public NetworkId RemapTo;
    }

    public struct JoinedClient : IComponentData
    {
        public Entity PlayerEntity;
    }

    public struct SpawnCharacter : IComponentData
    {
        public Entity ClientEntity;
        public float Delay;
    }
}
