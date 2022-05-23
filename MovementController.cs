using System.Collections;
using System.Collections.Generic;
using Mirror;
using SpaceApple.EasyPackets;
using UnityEngine;


namespace SpaceApple.MultiRoom
{

    /// <summary>
    /// Enables player character to be moved
    /// </summary>
    public class MovementController : NetworkBehaviour {
        private CharacterController _characterController;

        public float Speed = 3.0F;
        public float RotationSpeed = 3.0F;

        private bool _automateMovement;
	
        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        public void AutomateMovement()
        {
            _automateMovement = true;
        }

        public override void OnStartAuthority()
        {
            base.OnStartAuthority();

            if (ArgsParser.IsProvided("-autoPlay"))
            {
                AutomateMovement();
            }
        }

        void Update () {
            if (!isLocalPlayer)
                return;

            var forwardInput = _automateMovement ? 1 : Input.GetAxis("Vertical");
            var rotationInput = _automateMovement ? 1 : Input.GetAxis("Horizontal");
		
            // Rotate around y - axis
            transform.Rotate(0, rotationInput * RotationSpeed, 0);
		
            // Move forward / backward
            var forward = transform.TransformDirection(Vector3.forward);
            var curSpeed = Speed * forwardInput;
            _characterController.SimpleMove(forward * curSpeed);
        }
    }

}