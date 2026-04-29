using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PlayerBlock
{
    public sealed class MinionStageSealController : MonoBehaviour
    {
        [SerializeField] private Transform arenaRoot;
        [SerializeField] private Transform defeatSeal;
        [SerializeField] private Rigidbody sealRigidbody;
        [SerializeField] private float sealSpawnHeight = 18.5f;
        [SerializeField] private float sealLandingHeight = 0.92f;
        [SerializeField] private float sealDropSpeed = 22f;
        [SerializeField] private float clearInteractDistance = 5.5f;
        [SerializeField] private float sealPushRadius = 3.2f;
        [SerializeField] private float sealPushForce = 8.5f;
        [SerializeField] private float sealPushUpward = 0.8f;

        private bool _hasSeenMinion;
        private bool _sealDropping;
        private bool _sealReady;
        private bool _sealCleared;
        private Vector3 _sealTargetPosition;

        private void Awake()
        {
            if (arenaRoot == null)
            {
                var arenaObject = GameObject.Find("arena");
                arenaRoot = arenaObject != null ? arenaObject.transform : null;
            }

            if (defeatSeal == null)
            {
                var sealObject = GameObject.Find("seal");
                if (sealObject == null)
                {
                    sealObject = GameObject.Find("Seal");
                }

                defeatSeal = sealObject != null ? sealObject.transform : null;
            }

            if (defeatSeal != null)
            {
                if (sealRigidbody == null)
                {
                    sealRigidbody = defeatSeal.GetComponent<Rigidbody>();
                    if (sealRigidbody == null)
                    {
                        sealRigidbody = defeatSeal.gameObject.AddComponent<Rigidbody>();
                    }
                }

                PrepareSealAtRest();
            }
        }

        private void Update()
        {
            if (_sealCleared || BrowserPauseMenu.IsPaused)
            {
                return;
            }

            _hasSeenMinion |= ShadowMinionController.ActiveInstances.Count > 0;
            if (!_hasSeenMinion || HasActiveBoss())
            {
                return;
            }

            if (!_sealDropping && !_sealReady)
            {
                if (AreAllMinionsCleared())
                {
                    BeginSealDrop();
                }

                return;
            }

            if (_sealDropping)
            {
                UpdateSealDrop(Time.deltaTime);
            }

            if (_sealReady)
            {
                UpdateClearInteraction();
            }
        }

        private bool AreAllMinionsCleared()
        {
            var minions = ShadowMinionController.ActiveInstances;
            for (var i = 0; i < minions.Count; i++)
            {
                var minion = minions[i];
                if (minion != null && minion.IsAlive)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasActiveBoss()
        {
            var bosses = GiantBossController.ActiveInstances;
            for (var i = 0; i < bosses.Count; i++)
            {
                var boss = bosses[i];
                if (boss != null && boss.Health > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        private void BeginSealDrop()
        {
            if (defeatSeal == null)
            {
                return;
            }

            _sealTargetPosition = new Vector3(defeatSeal.position.x, sealLandingHeight, defeatSeal.position.z);
            EnableSealDropPhysics();
            _sealDropping = true;
            _sealReady = true;
            CombatHud.Instance.SetStatusMessage("APPROACH THE SEAL", true);
        }

        private void UpdateSealDrop(float deltaTime)
        {
            if (defeatSeal == null || sealRigidbody == null)
            {
                _sealDropping = false;
                return;
            }

            PushNearbyActorsAway();

            if (defeatSeal.position.y > _sealTargetPosition.y + 0.08f)
            {
                return;
            }

            defeatSeal.position = _sealTargetPosition;
            sealRigidbody.linearVelocity = Vector3.zero;
            sealRigidbody.angularVelocity = Vector3.zero;
            PrepareSealAtRest();
            _sealDropping = false;
            CombatVfxUtility.SpawnDustBurst(_sealTargetPosition, Vector3.up, 0.8f, 14);
            CombatHud.Instance.SetStatusMessage("APPROACH THE SEAL", true);
        }

        private void PushNearbyActorsAway()
        {
            var sealPosition = defeatSeal.position;
            PushPlayersAway(sealPosition);
            PushShadowsAway(sealPosition);
        }

        private void PushPlayersAway(Vector3 sealPosition)
        {
            var players = BlockPlayerController.ActiveInstances;
            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player == null || player.Health <= 0f)
                {
                    continue;
                }

                var controller = player.GetComponent<CharacterController>();
                if (controller == null)
                {
                    continue;
                }

                var playerPosition = player.transform.position;
                var away = playerPosition - sealPosition;
                away.y = 0f;
                var distance = away.magnitude;
                if (distance > sealPushRadius || distance < 0.001f)
                {
                    continue;
                }

                var push = away / distance * Mathf.Lerp(sealPushForce, 0.1f, distance / sealPushRadius);
                push.y = sealPushUpward;
                controller.Move(push * Time.deltaTime);
            }
        }

        private void PushShadowsAway(Vector3 sealPosition)
        {
            var shadows = ShadowCloneTarget.ActiveInstances;
            for (var i = 0; i < shadows.Count; i++)
            {
                var shadow = shadows[i];
                if (shadow == null || !shadow.IsAlive)
                {
                    continue;
                }

                var shadowTransform = shadow.transform;
                var away = shadowTransform.position - sealPosition;
                away.y = 0f;
                var distance = away.magnitude;
                if (distance > sealPushRadius || distance < 0.001f)
                {
                    continue;
                }

                var rigidbody = shadow.GetComponent<Rigidbody>();
                if (rigidbody == null || rigidbody.isKinematic)
                {
                    continue;
                }

                var pushDirection = away / distance;
                var pushVelocity = pushDirection * Mathf.Lerp(sealPushForce, 0.1f, distance / sealPushRadius);
                pushVelocity.y = sealPushUpward * 0.6f;
                rigidbody.linearVelocity = new Vector3(pushVelocity.x, Mathf.Max(rigidbody.linearVelocity.y, pushVelocity.y), pushVelocity.z);
            }
        }

        private void UpdateClearInteraction()
        {
            if (defeatSeal == null)
            {
                return;
            }

            var interactionPoint = defeatSeal.position;
            var hasNearbyPlayer = false;
            var players = BlockPlayerController.ActiveInstances;
            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player == null || player.Health <= 0f)
                {
                    continue;
                }

                if (Vector3.Distance(player.transform.position, interactionPoint) <= clearInteractDistance)
                {
                    hasNearbyPlayer = true;
                    break;
                }
            }

            CombatHud.Instance.SetStatusMessage(hasNearbyPlayer ? "PRESS E" : "APPROACH THE SEAL", true);
            if (!hasNearbyPlayer || !InteractPressed())
            {
                return;
            }

            CombatHud.Instance.SetStatusMessage(string.Empty, false);
            CombatHud.Instance.PlayEndingSequence();
            _sealCleared = true;
        }

        private static bool InteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        private void PrepareSealAtRest()
        {
            if (sealRigidbody == null)
            {
                return;
            }

            sealRigidbody.useGravity = false;
            sealRigidbody.isKinematic = true;
            sealRigidbody.linearVelocity = Vector3.zero;
            sealRigidbody.angularVelocity = Vector3.zero;
            sealRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private void EnableSealDropPhysics()
        {
            if (sealRigidbody == null)
            {
                return;
            }

            sealRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            sealRigidbody.isKinematic = false;
            sealRigidbody.useGravity = true;
            sealRigidbody.linearVelocity = Vector3.zero;
            sealRigidbody.angularVelocity = Vector3.zero;
        }
    }
}
