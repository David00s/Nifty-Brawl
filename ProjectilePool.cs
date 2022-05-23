using System.Collections.Generic;
using UnityEngine;


namespace SpaceApple.MultiRoom
{

    /// <summary>
    /// Manages the pool of projectiles
    /// </summary>
    public class ProjectilePool: MonoBehaviour
    {
        public static ProjectilePool Instance;

        public Projectile ProjectileProp;

        private Stack<Projectile> _pool;
    
        void Awake()
        {
            Instance = this;
        
            _pool = new Stack<Projectile>();
            ProjectileProp.gameObject.SetActive(false);
        }

        /// <summary>
        /// Moves projectile from a given point to a given direction at the given force
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <param name="force"></param>
        /// <param name="attacker"></param>
        public void LaunchProjectile(Vector3 origin, Vector3 direction, float force, uint attacker)
        {
            var projectile = GetProjectile();
            projectile.gameObject.SetActive(true);
            projectile.transform.position = origin;

            // Shoot the projectile
            projectile.Shoot(direction, force, attacker, () =>
            {
                // And store it back to the pool after it explodes
                projectile.gameObject.SetActive(false);
                _pool.Push(projectile);
            });
        }

        /// <summary>
        /// Returns existing projectile from the pool or instantiates a new one
        /// </summary>
        /// <returns></returns>
        public Projectile GetProjectile()
        {
            if (_pool.Count > 0)
                return _pool.Pop();

            return Instantiate(ProjectileProp, transform);
        }
    
    }

}