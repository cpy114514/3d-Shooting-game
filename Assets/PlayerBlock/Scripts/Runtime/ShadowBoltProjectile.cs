using UnityEngine;

namespace PlayerBlock
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class ShadowBoltProjectile : MonoBehaviour
    {
        [SerializeField] private float damage = 1f;
        [SerializeField] private float lifeTime = 2.2f;

        private Rigidbody _rigidbody;
        private float _age;

        public void Launch(Vector3 velocity, float boltDamage)
        {
            damage = boltDamage;
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.linearVelocity = velocity;
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= lifeTime)
            {
                Destroy(gameObject);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            var boss = collision.collider.GetComponentInParent<GiantBossController>();
            if (boss != null)
            {
                boss.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}
