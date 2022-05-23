using System;
using System.Collections;
using Mirror;
using SpaceApple.EasyPackets;
using UnityEngine;

using UnityEngine.UI;

namespace SpaceApple.MultiRoom
{
    public delegate void DeathHandler(PvpPlayer deadPlayer, PvpPlayer killer);

    /// <summary>
    /// Player character 
    /// </summary>
    public class PvpPlayer : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnHealthChanged))]
        public int Health;

        [SyncVar(hook = nameof(OnGunChargingChanged))]
        public bool IsGunCharging;

        [SyncVar]
        public string Username;
    
        public ProgressBar HealthBar;

        public bool IsDead { get; private set; }

        public Transform GunPointer;
        public ParticleSystem GunChargeParticles;

        public bool CanTakeDamage = false;
    
        public event DeathHandler Killed;

        public GameObject CurrentPlayerIndicator;

        public Transform NameLabel;
        public Text NameLabelText;

        private Transform _cameraTransform;
    
        void Awake()
        {
            _cameraTransform = Camera.main.transform;
            GunChargeParticles.gameObject.SetActive(false);
        }
    
        private void OnHealthChanged(int oldHealth, int newHealth)
        {
            Health = newHealth;
    
            HealthBar.Set(newHealth, 100);
        }

        public override void OnStartAuthority()
        {
            // Enable shooting
            StartCoroutine(DoShootingMechanics());
        
            // Enable the indicator for current player
            CurrentPlayerIndicator.SetActive(true);
        }

        /// <summary>
        /// Standard UNet method which is invoked when this script starts on clients
        /// </summary>
        public override void OnStartClient()
        {
            HealthBar.Set(Health, 100);
            NameLabelText.text = Username;
        }

        /// <summary>
        /// Standard UNet method which is invoked when this script starts on server
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();

            if (Ep.Runtime.IsBatchmode)
            {
                // Disable name label if server is running in batchmode
                // Moving many world-space UI elements on server drains CPU. 
                NameLabel.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            // Ignore if dead
            if (IsDead) return;
        
            // Update gun charge
            if (IsGunCharging)
            {
                var oldScale = GunChargeParticles.transform.localScale.x;
                var newScale = Mathf.Min(oldScale + Time.deltaTime, 1);
                GunChargeParticles.transform.localScale = Vector3.one * newScale;
            }
        
            // Kill player if he falls down (somehow)
            if (isServer && transform.position.y < -10f)
            {
                Kill(null);
                return;
            }
        
            NameLabel.forward = _cameraTransform.transform.forward;
        
            if (!isLocalPlayer)
                return;

        }

        /// <summary>
        /// Using coroutine to make it easier and more convenient to work with cooldowns and force increases.
        /// It runs for as long as the player is alive
        /// </summary>
        /// <returns></returns>
        private IEnumerator DoShootingMechanics()
        {
            const float lowestForce = 100f;
            const float maxForce = 500f;
        
            const float cooldown = 0.5f;
        
            while (true)
            {
                if (IsDead) yield break;
            
                var charge = 0f;
            
                if (Input.GetKey(KeyCode.Space))
                {
                    CmdUpdateChargingStatus(true);
                
                    // Start increasing the force of the shot  
                    while (Input.GetKey(KeyCode.Space))
                    {
                        charge = Mathf.Min(charge + Time.deltaTime, 1);
                        yield return null;
                    }

                    CmdUpdateChargingStatus(false);
                
                    var force = lowestForce + charge * (maxForce - lowestForce);
                    CmdReleaseProjectile(force, GunPointer.position, GunPointer.forward);
                
                    // Shot released, wait for the cooldown
                    yield return new WaitForSeconds(cooldown);
                }
            
                // Wait for the next frame
                yield return null; 
            }
        }

        /// <summary>
        /// Sends a command to server, so that server updates charging status on all clients (by changing syncvar value).
        /// This way, all of the clients will see when a player is charging
        /// </summary>
        /// <param name="isCharging"></param>
        [Command]
        private void CmdUpdateChargingStatus(bool isCharging)
        {
            IsGunCharging = isCharging;
        }

        /// <summary>
        /// Sends a command to server to release projectile from <see cref="origin"/> to <see cref="direction"/>
        /// </summary>
        /// <param name="force"></param>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        [Command]
        private void CmdReleaseProjectile(float force, Vector3 origin, Vector3 direction)
        {
            // Launch a projectile on the server
            ProjectilePool.Instance.LaunchProjectile(origin, direction, force, netId);
        
            // Broadcast it to all of the clients (for visuals only)
            RpcBroadcastProjectile(force, origin, direction);
        }

        /// <summary>
        /// Make sure that clients see the projectile flying
        /// </summary>
        [ClientRpc]
        private void RpcBroadcastProjectile(float force, Vector3 origin, Vector3 direction)
        {
            // Ignore if this client is running on server - it draws the projectiles itself
            if (isServer) return;
        
            ProjectilePool.Instance.LaunchProjectile(origin, direction, force, netId);
        }
    
        [Server]
        private void Kill(PvpPlayer attacker)
        {
            if (IsDead) return;

            if (Killed != null)
                Killed.Invoke(this, attacker);
        
            IsDead = true;
            StartCoroutine(DestroyAfterTime(0f));
        }
    
        /// <summary>
        /// Reduces health of the player or kills it, if player doesn't have enough health
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="attackerId"></param>
        public void TakeDamage(int damage, uint attackerId)
        {
            if (!CanTakeDamage)
                return;
        
            // Already dead
            if (Health <= 0)
                return;

            Health = Math.Max(Health - damage, 0);

            if (Health == 0)
            {
                var attacker = NetworkIdentity.spawned[attackerId];
                Kill(attacker == null ? null : attacker.GetComponent<PvpPlayer>());
            }
        }
    
        private IEnumerator DestroyAfterTime(float time)
        {
            yield return new WaitForSeconds(time);
            Destroy(gameObject);
        }

        /// <summary>
        /// Toggle gun charging display
        /// This will be called on every client when gun charging status changes
        /// </summary>
        /// <param name="isCharging"></param>
        private void OnGunChargingChanged(bool oldIsCharging, bool newIsCharging)
        {
            IsGunCharging = newIsCharging;
            
            if (newIsCharging)
            {
                GunChargeParticles.gameObject.SetActive(true);
                GunChargeParticles.transform.localScale = Vector3.zero;
            }
            else
            {
                GunChargeParticles.gameObject.SetActive(false);
            }
        }
    
    }

}