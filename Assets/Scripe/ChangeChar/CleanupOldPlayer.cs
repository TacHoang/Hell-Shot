using Fusion;
using static Unity.Collections.Unicode;

public class CleanupOldPlayer : NetworkBehaviour
{
    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            foreach (var obj in FindObjectsOfType<NetworkObject>())
            {
                if (obj != Object && obj.HasInputAuthority)
                {
                    Runner.Despawn(obj);
                }
            }
        }
    }
}