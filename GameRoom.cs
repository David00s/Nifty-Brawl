using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SpaceApple.EasyPackets;
using SpaceApple.NetworkingCore;
using UnityEngine;


namespace SpaceApple.MultiRoom
{
    /// <summary>
    /// This is one of the core scripts that should be added to any room template.
    /// It's mainly used to separate core room functionality on server side (trigger events, player lists and etc.)
    /// </summary>
    public class GameRoom: MonoBehaviour
    {
        /// <summary>
        /// Game room functionality that should be accessed on the server only
        /// </summary>
        public GameRoomOnServer Server { get; set; }

        /// <summary>
        /// Invoked when room is destroyed
        /// </summary>
        public event Action<GameRoom> RoomDestroyed;

        public int RoomId { get; set; }
    
        private static int _nextRoomId;
    
        void OnDestroy()
        {
            if (Ep.Server.IsServerRunning && Server != null)
            {
                // If it's a server, remove all of the players from the room
                Server.RemoveAllPlayers();
            }
        
            if (RoomDestroyed != null)
                RoomDestroyed.Invoke(this);
        }
    
        /// <summary>
        /// Should be called from the server-side to initialize the room
        /// </summary>
        public void SetupServerSide()
        {
            Server = new GameRoomOnServer(this); 
            RoomId = ++_nextRoomId;
        }

        public class GameRoomOnServer
        {
            private readonly GameRoom _gameRoom;
            private readonly GameRoom _room;
        
            /// <summary>
            /// Invoked when player is added to the room
            /// </summary>
            public event Action<EpPeer> PlayerAdded;
        
            /// <summary>
            /// Invoked when player is removed from the room
            /// </summary>
            public event Action<EpPeer> PlayerRemoved;
        
            /// <summary>
            /// Invoked when object is destroyed
            /// </summary>
            public event Action<NetworkIdentity> ObjectDestroyed;

            /// <summary>
            /// Invoked when object is spawned in this room
            /// </summary>
            public event Action<NetworkIdentity> ObjectSpawned; 

            private List<EpPeer> _players;
        
            private HashSet<NetworkIdentity> _spawnedObjects;

            public GameRoomOnServer(GameRoom gameRoom)
            {
                _gameRoom = gameRoom;
                _players = new List<EpPeer>();
                _spawnedObjects = new HashSet<NetworkIdentity>();
            }

            public IEnumerable<NetworkConnection> PlayerConnections
            {
                get { return _players.Select(p => p.Connection); }
            }

            public bool IsPlayerInRoom(NetworkConnection con)
            {
                return _players.Any(p => p.Connection == con);
            }
        
            public List<EpPeer> Players
            {
                get { return _players.ToList(); }
            }
        
            /// <summary>
            /// Adds a player to the room
            /// </summary>
            /// <param name="peer"></param>
            public void AddPlayer(EpPeer peer)
            {
                _players.Add(peer);

                peer.Disconnected += OnPeerDisconnected;
                peer.State.Set<GameRoom>(_gameRoom);

                RebuildObservers();

                if (PlayerAdded != null)
                    PlayerAdded.Invoke(peer);
            }

            /// <summary>
            /// Removes a player from the room
            /// </summary>
            /// <param name="peer"></param>
            public void RemovePlayer(EpPeer peer)
            {
                _players.Remove(peer);

                peer.Disconnected -= OnPeerDisconnected;
                peer.State.Set<GameRoom>(null);
            
                if (PlayerRemoved != null)
                    PlayerRemoved.Invoke(peer);
            }

            private void OnPeerDisconnected(EpPeer peer)
            {
                // Remove player from the room when he disconnects
                RemovePlayer(peer);
            }

            public void OnObjectSpawned(NetworkIdentity networkIdentity)
            {
                _spawnedObjects.Add(networkIdentity);
            
                if (ObjectSpawned != null)
                    ObjectSpawned.Invoke(networkIdentity);
            }
        
            public void OnObjectDestroyed(NetworkIdentity networkIdentity)
            {
                _spawnedObjects.Remove(networkIdentity);
            
                if (ObjectDestroyed != null) 
                    ObjectDestroyed.Invoke(networkIdentity);
            }

            /// <summary>
            /// This should be called every time you add a new player to the room.
            /// </summary>
            public void RebuildObservers()
            {
                foreach (var obj in _spawnedObjects)
                {
                    NetworkServer.RebuildObservers(obj, false);
                }
            }

            /// <summary>
            /// Removes all of the players from the room
            /// </summary>
            public void RemoveAllPlayers()
            {
                var players = _players.ToList();
                foreach (var player in players)
                {
                    RemovePlayer(player);
                }
            }

            /// <summary>
            /// Sends a message to all of the connected players
            /// </summary>
            /// <param name="opCode"></param>
            /// <param name="write"></param>
            /// <param name="channel"></param>
            public void BroadcastPacket(short opCode, Action<NetWriter> write, int channel = Ep.DefaultChannel)
            {
                foreach (var player in _players)
                {
                    player.Send(opCode, write, channel);
                }
            }
        
        }
    }

}