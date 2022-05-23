using System.Collections;
using SpaceApple.EasyPackets;
using SpaceApple.NetworkingCore;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceApple.MultiRoom
{

    /// <summary>
    /// All of the UI controls are implemented here
    /// </summary>
    public class PvpUi : MonoBehaviour {

        public ClientController ClientController;
        public string WinningMessage = "<color=#B5FF00ff>YOU WON!</color>";
        public string LosingMessage = "<color=#FF0300ff>YOU LOST :(</color>";
	
        public GameObject Menu;

        public InputField Username;
        public Text UsernameErrorText;
        public Button UsernameChangeButton;
	
        public static PvpUi Instance;

        public GameObject WinningStripe;
        public Text WinningText;

        public GameObject WaitingStripe;
        public Text WaitingText;

        public Text timerText;
        private Coroutine _timerCoroutine;

        public Button LeaveGameButton;

        public Text PlayerCount;
	
        void Awake()
        {
            Instance = this;
            Username.text = "Username-" + Random.Range(1000, 9999);
		
            // Wait for the connection to be established
            Ep.Client.Connected += OnConnectedToServer;
		
            // Handle incoming messages
            Ep.Client.SetHandler((short) PvpOpCodes.DisplayWaitingMessage, HandleDisplayWaitingMessage);
            Ep.Client.SetHandler((short) PvpOpCodes.StartTimer, HandleStartTimer);
            Ep.Client.SetHandler((short) PvpOpCodes.MatchFinished, HandleOneVsOneFinished);
            Ep.Client.SetHandler((short) PvpOpCodes.PlayerCountUpdate, HandlePlayerCountUpdate);
        }

        private void HandleOneVsOneFinished(EpMessage message)
        {
            var hasWon = message.Reader.ReadBoolean();
            WinningStripe.SetActive(true);
            WinningText.text = hasWon ? WinningMessage : LosingMessage;
        }

        /// <summary>
        /// Called when server tells client to display a timer
        /// </summary>
        /// <param name="message"></param>
        private void HandleStartTimer(EpMessage message)
        {
            var time = message.Reader.ReadSingle();

            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
            }
		
            _timerCoroutine = StartCoroutine(RunTimer(time));
        }

        /// <summary>
        /// Timer ticking functionality
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private IEnumerator RunTimer(float time)
        {
            timerText.gameObject.SetActive(true);

            var seconds = (int) time;
		
            while (time > 0)
            {
                yield return null;
                time -= Time.deltaTime;

                if ((int) time != seconds)
                {
                    // Update the text
                    seconds = (int) time;
                    timerText.text = string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);
                }

                if (seconds == 0)
                {
                    yield return new WaitForSeconds(1);
                    timerText.gameObject.SetActive(false);
                    yield break;
                }
            }
        }

        public void HideTimer()
        {
            timerText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Called when server sends a request for client to display a message (or hide it)
        /// </summary>
        /// <param name="message"></param>
        private void HandleDisplayWaitingMessage(EpMessage message)
        {
            var isVisible = message.Reader.ReadBoolean();

            if (!isVisible)
            {
                WaitingStripe.SetActive(false);
                return;
            }

            var text = message.Reader.ReadString();
            WaitingText.text = text;
            WaitingStripe.SetActive(true);
        }

        /// <summary>
        /// Called when client connects to server
        /// </summary>
        private void OnConnectedToServer()
        {
            OnChangeNameClick();
        }

        public void OnPlayClick()
        {
            ClientController.SendPlayRequest();
        }

        public void SetMenuVisibility(bool isVisible)
        {
            if (Menu)
            {
                Menu.gameObject.SetActive(isVisible);
            }
        }

        /// <summary>
        /// Sends a request to server to change the username
        /// </summary>
        public void OnChangeNameClick()
        {
            UsernameChangeButton.interactable = false;
            UsernameErrorText.gameObject.SetActive(false);
		
            var newUsername = Username.text;

            Ep.Client.Send((short) PvpOpCodes.ChangeUsername, w => w.Write(newUsername), response =>
            {
                if (response.Status != ResponseStatus.Success)
                {
                    UsernameErrorText.text = response.AsString();
                    UsernameErrorText.gameObject.SetActive(true);
                    return;
                }
			
            });
        }

        public void OnUsernameChanged()
        {
            UsernameChangeButton.interactable = true;
        }

        public void OnLeaveGameClick()
        {
            Ep.Client.Send((short) PvpOpCodes.LeaveGame);
        }
	
        private void HandlePlayerCountUpdate(EpMessage message)
        {
            PlayerCount.text = message.Reader.ReadInt32().ToString();
        }

    }

}