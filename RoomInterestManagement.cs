using System.Collections.Generic;
using Mirror;
using UnityEngine;


namespace SpaceApple.MultiRoom
{
    /// <summary>
    /// This script ensures that only players and objects in the same room can see each other.
    /// It separates players between rooms, so data from one room doesn't need to be sent to other rooms.
    /// </summary>
    public class RoomInterestManagement : InterestManagement
    {
        /// <summary>
        /// Normally, this method would be invoked on all networked objects to see if they should include the player with
        /// provided connection. However, this method gets called JUST when client connects to server, which is too
        /// early, and player is neither spawned nor yet assigned to a room.
        /// For this reason. we ignore this method, and manually rebuild observers when a new player is spawned in a room.
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        public override bool OnCheckObserver(NetworkIdentity identity, NetworkConnection newObserver)
        {
            return false;
        }

        /// <summary>
        /// Logic for building observers list for each object
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="newObservers"></param>
        /// <param name="initialize"></param>
        public override void OnRebuildObservers(NetworkIdentity identity, HashSet<NetworkConnection> newObservers, bool initialize)
        {
            var room = identity.GetComponent<RoomObject>()?.CurrentRoom;
            if (room == null)
                return;
		
            foreach (var connection in room.Server.PlayerConnections)
            {
                newObservers.Add(connection);
            }
        }
    }
}