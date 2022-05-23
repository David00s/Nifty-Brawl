using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SpaceApple.EasyPackets;
using SpaceApple.NetworkingCore;
using UnityEngine;

namespace SpaceApple.MultiRoom
{

    /// <summary>
    /// This is the core script of the game server,
    /// it handles requests from client to start a game, change username and etc.
    /// </summary>
    public class PvpGameServer: MonoBehaviour
    {
        public GameRoom RoomTemplate;
    
        public RoomPositioner RoomPositioner;

        private List<PvpModeController> _rooms;
    
        private EpNetworkManager _networkManager;

        private Coroutine _updatePlayerCountCoroutine;
    
        void Awake()
        {
            RoomPositioner = RoomPositioner ?? FindObjectOfType<RoomPositioner>();
        
            // Network manager setup
            _networkManager = FindObjectOfType<EpNetworkManager>();
            _networkManager.maxConnections = 2000;
        
            // FPS limiter (so that our server doesn't max out CPU for no reason)
            var fps = ArgsParser.ExtractValueInt("-fps", 60);
            Application.targetFrameRate = fps;
            QualitySettings.vSyncCount = 0;

            _rooms = new List<PvpModeController>();
        
            Ep.Server.Started += OnServerStarted;
            Ep.Server.ClientJoined += OnClientJoined;
            Ep.Server.ClientLeft += OnClientLeft;
        }

        void Start()
        {
            if (ArgsParser.IsProvided("-startServer"))
            {
                _networkManager.StartServer();
            }
        }

        public void OnServerStarted()
        {
            Ep.Server.SetHandler((short) PvpOpCodes.Play, HandlePlayRequest);
            Ep.Server.SetHandler((short) PvpOpCodes.ChangeUsername, HandleChangeUsername);
            Ep.Server.SetHandler((short) PvpOpCodes.ClientRoomInstantiated, OnClientRoomInstantiated);
            Ep.Server.SetHandler((short) PvpOpCodes.LeaveGame, HandleLeaveGame);
        }

        /// <summary>
        /// Handles a request from client to join a game
        /// </summary>
        /// <param name="message"></param>
        private void HandlePlayRequest(EpMessage message)
        {
            GameRoom room;        

            // Try to find an existing room
            var existingRoom = _rooms.FirstOrDefault(r => !r.IsLocked);

            if (existingRoom != null)
            {
                // Reuse the room if it exists
                room = existingRoom.GetComponent<GameRoom>();
            }
            else
            {
                // Or create a new one
                room = CreateRoom(RoomTemplate);
                room.SetupServerSide();
                room.gameObject.SetActive(true);
                room.RoomDestroyed += OnPvpRoomDestroyed;
                room.GetComponent<PvpModeController>().OnRoomCreatedInServer();
            
                // Add room to list (so that we can find it when another player joins)
                _rooms.Add(room.GetComponent<PvpModeController>());
            }

            // Add player to the room
            room.Server.AddPlayer(message.Peer);
        
            // Room created successfully 
            message.Respond(ResponseStatus.Success, writer =>
            {
                writer.Write(room.RoomId);
                writer.Write(room.transform.position);
            });
        }
    
        /// <summary>
        /// Handles a request from client to leave the game
        /// </summary>
        /// <param name="message"></param>
        private void HandleLeaveGame(EpMessage message)
        {
            var room = message.Peer.State.Get<GameRoom>();

            if (room == null)
            {
                message.Respond(ResponseStatus.Failed, "You're not in a room");
                return;
            }
        
            room.Server.RemovePlayer(message.Peer);
        }

        /// <summary>
        /// Handles a message from client, which basically says "I've instantiated the room and I'm ready to play"
        /// </summary>
        /// <param name="message"></param>
        private void OnClientRoomInstantiated(EpMessage message)
        {
            var room = message.Peer.State.Get<GameRoom>();

            if (room != null)
            {
                room.GetComponent<PvpModeController>().OnRoomInstantiatedOnClient(message.Peer);
            }
        }

        /// <summary>
        /// Handles request to change the username
        /// </summary>
        /// <param name="message"></param>
        private void HandleChangeUsername(EpMessage message)
        {
            var newUsername = message.Reader.ReadString().Trim();

            if (newUsername.Length < 3)
            {
                message.Respond(ResponseStatus.Failed, "Username is too short");
                return;
            }

            if (newUsername.Length > 16)
            {
                message.Respond(ResponseStatus.Failed, "Username is too long");
                return;
            }
        
            message.Peer.State.Set("username", newUsername);
        
            message.Respond(ResponseStatus.Success);
        }
    
        /// <summary>
        /// Called when room is destroyed
        /// </summary>
        /// <param name="room"></param>
        private void OnPvpRoomDestroyed(GameRoom room)
        {
            // Unsubscribe from listener
            room.RoomDestroyed -= OnPvpRoomDestroyed;
        
            // Free up room position
            RoomPositioner.FreeUpPosition(room.transform.position);

            // Remove from list
            _rooms.Remove(room.GetComponent<PvpModeController>());
        }

        /// <summary>
        /// Uses a template to instantiate a room
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        private GameRoom CreateRoom(GameRoom template)
        {
            var position = RoomPositioner.GetPositionForRoom(template);

            if (!position.HasValue)
                return null;
        
            var room = Instantiate(template, RoomPositioner.transform);
            room.gameObject.SetActive(true);
            room.transform.position = position.Value;
            return room;
        }

        private IEnumerator LimitPlayerCountUpdate()
        {
            yield return new WaitForSeconds(0.5f);

            _updatePlayerCountCoroutine = null;
        
            var count = Ep.Server.Peers.Count;
            foreach (var player in Ep.Server.Peers.Values)
            {
                player.Send((short) PvpOpCodes.PlayerCountUpdate, w => w.Write(count));
            }
        }
    
        private void OnClientJoined(EpPeer peer)
        {
            if (_updatePlayerCountCoroutine == null)
            {
                _updatePlayerCountCoroutine = StartCoroutine(LimitPlayerCountUpdate());
            }
        }
    
        private void OnClientLeft(EpPeer peer)
        {
            if (_updatePlayerCountCoroutine == null)
            {
                _updatePlayerCountCoroutine = StartCoroutine(LimitPlayerCountUpdate());
            }
        }
    }

}