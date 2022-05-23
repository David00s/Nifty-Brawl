using System;
using System.Collections;
using System.Linq;
using Mirror;
using SpaceApple.EasyPackets;
using SpaceApple.NetworkingCore;
using UnityEngine;

using Random = UnityEngine.Random;

namespace SpaceApple.MultiRoom
{
    /// <summary>
    /// Hold main client-side code
    /// </summary>
    public class ClientController: MonoBehaviour
    {
        public NetworkManager NetworkManager;
        public Transform HomeCameraPosition;
        public GameRoom RoomTemplate;
    
        void Awake()
        {
            if (ArgsParser.IsProvided("-connectTo"))
            {
                NetworkManager.networkAddress = ArgsParser.ExtractValue("-connectTo");
                NetworkManager.StartClient();
            }
        
            // Wait for the connection to be established
            Ep.Client.Connected += OnConnectedToServer;
            Ep.Client.Disconnected += OnClientDisconnected;
            Ep.Client.SetHandler((short) PvpOpCodes.RemovedFromRoom, HandleRemovedFromRoom);
        }

        private void OnConnectedToServer()
        {
            if (ArgsParser.IsProvided("-autoPlay"))
            {
                // If auto play argument is provided, start the match automatically
                // but before doing that, wait a random interval (looks visually better when players are spread out a bit)
                var randomDelay = (float) (0.2 + Random.value * 2);
                StartCoroutine(ExecuteAfterDelay(SendPlayRequest, randomDelay));
            }
        }

        /// <summary>
        /// Sends a request to server to play the game
        /// </summary>
        public void SendPlayRequest()
        {
            // Send a request to server to start a game
            Ep.Client.Send((short) PvpOpCodes.Play, (w) => {}, response =>
            {
                if (response.Status != ResponseStatus.Success)
                {
                    Debug.LogError("Failed to join a room: " + response.AsString());
                    return;
                }
            
                // Hide menu
                PvpUi.Instance.SetMenuVisibility(false);
            
                // Display leave game button
                PvpUi.Instance.LeaveGameButton.gameObject.SetActive(true);
        
                // Instantiate room
                var room = CreateRoomFromResponse(response, RoomTemplate);
            
                // Send notification to let server know that client has instantiated the room
                Ep.Client.Send((short) PvpOpCodes.ClientRoomInstantiated);
            
                // Move camera to room
                MoveCameraTo(room.GetComponent<PvpModeController>().RoomCameraPosition);
            });
        }
    
        /// <summary>
        /// This will be invoked when player is removed from a room
        /// </summary>
        /// <param name="message"></param>
        private void HandleRemovedFromRoom(EpMessage message)
        {
            var roomId = message.Reader.ReadInt32();
            var room = FindObjectsOfType<GameRoom>().FirstOrDefault(r => r.RoomId == roomId);
        
            if (room && !Ep.Server.IsServerRunning)
            {
                // Destroy the room we're leaving (only if we're not running this on server (a.k.a host))
                Destroy(room.gameObject);
            }
		
            PvpUi.Instance.LeaveGameButton.gameObject.SetActive(false);
            PvpUi.Instance.WaitingStripe.SetActive(false);
            PvpUi.Instance.WinningStripe.SetActive(false);
            PvpUi.Instance.Menu.SetActive(true);
        
            MoveCameraTo(HomeCameraPosition);

            if (ArgsParser.IsProvided("-autoPlay"))
            {
                // Start another round
                SendPlayRequest();
            }
        }
    
        /// <summary>
        /// Reads response and creates a room, by using a specified prefab
        /// </summary>
        /// <param name="response"></param>
        /// <param name="roomPrefab"></param>
        public GameRoom CreateRoomFromResponse(EpMessage response, GameRoom roomPrefab)
        {
            var roomId = response.Reader.ReadInt32();
            var roomPosition = response.Reader.ReadVector3();
            GameRoom room = null;
			
            if (!Ep.Server.IsServerRunning)
            {
                // Only instantiate if it's not server (host),
                // because host will already have an instance of the room created
                room = Instantiate(roomPrefab);
                room.transform.position = roomPosition;
                room.gameObject.SetActive(true);
            }
            else
            {
                room = FindObjectsOfType<GameRoom>().FirstOrDefault(r => r.RoomId == roomId);
            }

            room.RoomId = roomId;
            return room;
        }
    
        public void MoveCameraTo(Transform target)
        {
            if (Camera.main)
            {
                Camera.main.transform.position = target.position;
                Camera.main.transform.rotation = target.rotation;
            }
        }

        private IEnumerator ExecuteAfterDelay(Action callback, float delay)
        {
            yield return new WaitForSeconds(delay);
            callback();
        }
    
        private void OnClientDisconnected()
        {
            if (ArgsParser.IsProvided("-autoPlay"))
            {
                // Close the application on disconnect, if autoplay arg is provided.
                // This way, I won't need to close all of the clients when performance testing
                // as they will all close when I shut down the server
                Application.Quit();
            }
        }
    }

}