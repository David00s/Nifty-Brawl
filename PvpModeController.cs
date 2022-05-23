using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SpaceApple.EasyPackets;
using UnityEngine;


namespace SpaceApple.MultiRoom
{
    /// <summary>
    /// Controls game logic in PvP room
    /// </summary>
    public class PvpModeController: MonoBehaviour
    {
        [Tooltip("Position, to which camera will be moved when room is instantiated on client")]
        public Transform RoomCameraPosition;
    
        [Tooltip("Players will be spawned in this position")]
        public Transform SpawnPoint;
    
        [Tooltip("Player character object")]
        public PvpPlayer PlayerPrefab;
    
        /// <summary>
        /// If true, no more players will be added to this room
        /// </summary>
        public bool IsLocked { get; protected set; }

        private GameRoom _gameRoom;
        private bool _isGameOver;
        private int _roomSize = 2;
    
        /// <summary>
        /// Called on server, when server instantiates the room
        /// </summary>
        public virtual void OnRoomCreatedInServer()
        {
            _gameRoom = GetComponent<GameRoom>();
            _gameRoom.Server.PlayerAdded += OnPlayerAdded;
            _gameRoom.Server.PlayerRemoved += OnPlayerRemoved;
            _gameRoom.Server.ObjectDestroyed += OnObjectDestroyed;

            if (ArgsParser.IsProvided("-roomSize"))
            {
                // This is for performance testing, to see what's more optimal - many small rooms 
                // or fewer larger rooms
                _roomSize = ArgsParser.ExtractValueInt("-roomSize");
            }
        }
    
        protected virtual IEnumerator GameLoop()
        {
            // Start timer on clients
            _gameRoom.Server.BroadcastPacket((short) PvpOpCodes.StartTimer, w => w.Write(4f));
        
            // Wait for the time to pass
            yield return new WaitForSeconds(4f);
        
            // --------------------------------------
            // Enable damage to each player
            foreach (var character in GetPlayerCharacters())
            {
                character.CanTakeDamage = true;
            }
        
            // --------------------------------------
            // Wait for game to be over
            while(!_isGameOver) yield return null;

            // --------------------------------------
            // Send game over updates to players
            foreach (var player in _gameRoom.Server.Players)
            {
                var playerObj = player.State.Get<PvpPlayer>();
                // Player who is still alive wins
                var hasWon = playerObj && !playerObj.IsDead;
                player.Send((short) PvpOpCodes.MatchFinished, w => w.Write(hasWon));
            }
        
            // --------------------------------------
            // Wait a few seconds and destroy the room
            yield return new WaitForSeconds(5f);

            // Destroy room
            Destroy(gameObject);
        }
    
        /// <summary>
        /// Spawns a player in this room
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="spawnPoint"></param>
        /// <returns></returns>
        public PvpPlayer SpawnPlayer(EpPeer peer, Transform spawnPoint)
        {
            // Spawn the player
            var player = Instantiate(PlayerPrefab);
            player.Username = peer.State.Get("username", "Unknown"); 
            player.gameObject.SetActive(true);
            player.transform.position = spawnPoint.position;
            player.GetComponent<RoomObject>().CurrentRoom = _gameRoom;
            NetworkServer.ReplacePlayerForConnection(peer.Connection, player.gameObject, true);

            player.Killed += OnPlayerKilled;
            player.CanTakeDamage = false;
        
            // Save the player game object in the peer's state
            peer.State.Set(player);
        
            return player;
        }

        /// <summary>
        /// Called when one of the players is killed by another player (or suicide)
        /// </summary>
        /// <param name="deadplayer"></param>
        /// <param name="killer"></param>
        protected virtual void OnPlayerKilled(PvpPlayer deadplayer, PvpPlayer killer)
        {
            // Game is over when one of the players is dead
            _isGameOver = true;
        }

        /// <summary>
        /// Called when any of the objects within room is destroyed
        /// </summary>
        /// <param name="obj"></param>
        protected virtual void OnObjectDestroyed(NetworkIdentity obj)
        {
            // Game is over when one of the objects is destroyed for any reason
            _isGameOver = true;
        }
    
        /// <summary>
        /// Called when player is removed from the room
        /// </summary>
        /// <param name="peer"></param>
        protected virtual void OnPlayerRemoved(EpPeer peer)
        {
            peer.Send((short) PvpOpCodes.RemovedFromRoom, w => w.Write(_gameRoom.RoomId));
        
            // Unset current room
            peer.State.Set<PvpModeController>(null);
        
            var playerObj = peer.State.Get<PvpPlayer>();
            if (playerObj)
            {
                // Destroy player object 
                Destroy(playerObj.gameObject);
            }

            // Cleanup 
            peer.State.Set<PvpPlayer>(null);
        
            // Destroy the room if it becomes empty
            if (_gameRoom.Server.Players.Count == 0)
                Destroy(gameObject);
        }

        /// <summary>
        /// Called when player is added to the room
        /// </summary>
        /// <param name="peer"></param>
        protected virtual void OnPlayerAdded(EpPeer peer)
        {
            // Save current room
            peer.State.Set<PvpModeController>(this);
        
            // Spawn the player
            SpawnPlayer(peer, SpawnPoint);

            if (_gameRoom.Server.Players.Count != _roomSize)
                return;
        
            // Lock the room so no more players can join 
            IsLocked = true;
        
            // Start the game loop
            StartCoroutine(GameLoop());
        }

        /// <summary>
        /// Called when server handles a notification from server, saying that
        /// client has successfully instantiated the room
        /// </summary>
        /// <param name="peer"></param>
        public virtual void OnRoomInstantiatedOnClient(EpPeer peer)
        {
            if (_gameRoom.Server.Players.Count < 2)
            {
                // If there's only one player, let him know that he needs to wait for an opponent
                var message = "Waiting for opponent";
                peer.Send((short) PvpOpCodes.DisplayWaitingMessage, w => w.Write(true).Write(message));
            }
            else
            {
                // No one is waiting for anything anymore, so hide the waiting message
                _gameRoom.Server.BroadcastPacket((short) PvpOpCodes.DisplayWaitingMessage, w => w.Write(false));
            }
        }
    
        private List<PvpPlayer> GetPlayerCharacters()
        {
            return _gameRoom.Server.Players
                .Select(p => p.State.Get<PvpPlayer>())
                .Where(p => p)
                .ToList();
        }
    }

}