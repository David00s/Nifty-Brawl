using System;
using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;


namespace SpaceApple.MultiRoom
{

    /// <summary>
    /// Controls the movement of projectile and its explosion
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public Rigidbody Rigidbody;
        public ParticleSystem Explosion;
        public MeshRenderer Renderer;
        public Collider Collider;
    
        private uint _attackerId;
    
        private Action _doneCallback;
    
        /// <summary>
        /// Curve is used to change damage depending on the distance from the targer
        /// </summary>
        [Header("Dynamic damage")]
        public AnimationCurve DamageCurve = AnimationCurve.Linear(0, 50, 4, 0);
    
        [Header("Layers (for physics)")]
        public int PlayerLayer = 9;
        public int ProjectileLayer = 10;

        void Awake()
        {
            // Make sure projectiles don't collide with players (so that we don't need to handle
            // latency related issues)
            // Normally this would be done in Physics editor window, but to avoid having to include project settings in this
            // packet, this will do.
            // Note: Explosions will still affect players - collision physics are not used.
            Physics.IgnoreLayerCollision(PlayerLayer, ProjectileLayer);
        
            // Don't allow projectiles to collide together
            Physics.IgnoreLayerCollision(ProjectileLayer, ProjectileLayer);
        }
    
        /// <summary>
        /// Adds force to the projectile and makes it fly forward
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="force"></param>
        /// <param name="attackerId"></param>
        /// <param name="doneCallback"></param>
        public void Shoot(Vector3 direction, float force, uint attackerId, Action doneCallback)
        {
            _attackerId = attackerId;
            _doneCallback = doneCallback;
            Rigidbody.velocity = Vector3.zero;
            Renderer.enabled = true;
            Rigidbody.isKinematic = false;
            Rigidbody.AddForce(direction * force);

            StartCoroutine(StartAutoDestruct(3));
            Collider.enabled = true;
        }
    
        void OnCollisionEnter(Collision collision)
        {
            Rigidbody.isKinematic = true;
            Collider.enabled = false;
            StartCoroutine(StartExplosion());
        }
    
        private IEnumerator StartExplosion()
        {
            // Hide the projectile
            Renderer.enabled = false;
            
            // Play explosion particles
            Explosion.Play();

            CalculateDamage();
        
            Collider.enabled = false;

            // Wait for explosion to finish
            yield return new WaitForSeconds(1f);
        
            // Trigger callback which will store this projectile to the pool
            if (_doneCallback != null)
                _doneCallback.Invoke();
        }
    
        /// <summary>
        /// Checks the area around to see if any of the objects around should be damaged
        /// </summary>
        public void CalculateDamage()
        {
            // Ignore if this is not a server
            if (!NetworkServer.active) return;

            var maxDamageArea = DamageCurve.keys.Last().time;
        
            var hitColliders = Physics.OverlapSphere(transform.position, maxDamageArea, 1 << PlayerLayer);
        
            foreach (var collider in hitColliders)
            {
                // Evaluate damage depending on the distance
                var damage = DamageCurve.Evaluate(Vector3.Distance(transform.position, collider.transform.position));
            
                var player = collider.GetComponent<PvpPlayer>();
                if (!player) continue;
            
                player.TakeDamage((int) damage, _attackerId);
            }
        }
    
        private IEnumerator StartAutoDestruct(float time)
        {
            yield return new WaitForSeconds(time);

            yield return StartExplosion();
        }
    }

}