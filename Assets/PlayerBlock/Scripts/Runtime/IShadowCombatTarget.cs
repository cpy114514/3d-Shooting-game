using UnityEngine;

namespace PlayerBlock
{
    public interface IShadowCombatTarget
    {
        bool IsTargetAlive { get; }
        Transform TargetTransform { get; }
        Vector3 GetAimPoint();
        bool TryGetClosestPoint(Vector3 fromPosition, out Vector3 closestPoint);
        void ReceiveShadowDamage(float amount);
    }

    public static class ShadowCombatTargetUtility
    {
        public static IShadowCombatTarget FindClosestTarget(Vector3 origin)
        {
            IShadowCombatTarget bestTarget = null;
            var bestSqrDistance = float.PositiveInfinity;

            var bosses = GiantBossController.ActiveInstances;
            for (var i = 0; i < bosses.Count; i++)
            {
                var boss = bosses[i];
                if (boss == null || !boss.IsTargetAlive)
                {
                    continue;
                }

                var sqrDistance = (boss.transform.position - origin).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestTarget = boss;
                }
            }

            var minions = ShadowMinionController.ActiveInstances;
            for (var i = 0; i < minions.Count; i++)
            {
                var minion = minions[i];
                if (minion == null || !minion.IsTargetAlive)
                {
                    continue;
                }

                var sqrDistance = (minion.transform.position - origin).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestTarget = minion;
                }
            }

            return bestTarget;
        }

        public static IShadowCombatTarget ResolveTarget(Collider collider)
        {
            if (collider == null)
            {
                return null;
            }

            var boss = collider.GetComponentInParent<GiantBossController>();
            if (boss != null && boss.IsTargetAlive)
            {
                return boss;
            }

            var minion = collider.GetComponentInParent<ShadowMinionController>();
            if (minion != null && minion.IsTargetAlive)
            {
                return minion;
            }

            return null;
        }
    }
}
