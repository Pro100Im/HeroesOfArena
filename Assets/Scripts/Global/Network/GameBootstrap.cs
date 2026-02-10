using Unity.NetCode;
using UnityEngine.Scripting;

namespace Global
{
    [Preserve]
    public class GameBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {            
            return false;
        }
    }
}