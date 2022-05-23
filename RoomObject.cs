using Mirror;
using UnityEngine;

namespace SpaceApple.MultiRoom
{
    /// <summary>
    /// Should be added to all the networked objects in a room (players and etc.)
    /// </summary>
    public class RoomObject: NetworkBehaviour
    {
        public GameRoom CurrentRoom { get; set; }

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            if (!CurrentRoom)
            {
                Debug.LogError("OnStartServer invoked, but the CurrentRoom property was not set. Make sure you set" +
                               " the CurrentRoom on objects that you spawn as soon as possible.");
                return;
            }
            
            CurrentRoom.Server.OnObjectSpawned(GetComponent<NetworkIdentity>());
        }
        
        void OnDestroy()
        {
            if (CurrentRoom == null)
                return;

            // Notify the room about destroyed game object
            CurrentRoom.Server.OnObjectDestroyed(GetComponent<NetworkIdentity>());
        }
    }
}