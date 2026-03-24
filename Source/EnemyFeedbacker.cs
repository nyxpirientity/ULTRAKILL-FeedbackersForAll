using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nyxpiri.ULTRAKILL.NyxLib;
using UnityEngine;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    public static class EnemyFeedbackerEnemyComponentsExtension
    {
        public static EnemyFeedbacker GetFeedbacker(this EnemyComponents enemy)
        {
            return enemy.GetMonoByIndex<EnemyFeedbacker>(EnemyFeedbacker.MonoRegistrarIndex);
        }
    }

    public class EnemyFeedbacker : MonoBehaviour
    {
        private EnemyComponents _eadd = null;
        public bool Enabled { get => NyxLib.Cheats.IsCheatEnabled(Cheats.FeedbackersForEveryone); }
        public bool ReadyToParry 
        { 
            get
            {
                return LastParryTimestamp.TimeSince >= ParryCooldown && Stamina >= ParryCost && Enabled;
            } 
        }

        public float ParryCost { get => (float)Options.ParryStaminaCost[_enemyType].Value; }
        public float ParryCooldown { get => (float)Options.MinParryCooldowns[_enemyType].Value; }
        public FixedTimeStamp LastParryTimestamp;
        public float Stamina { get; private set; } = 0;
        public static int MonoRegistrarIndex { get; private set; }

        public void QueueParry(Action<Vector3> parryAction)
        {
            var initTime = new FixedTimeStamp();
            initTime.UpdateToNow();
            _queuedParries.Add(new QueuedParry { ParryAction = parryAction, InitTime = initTime, PosAtTheTime = transform.position });
        }

        protected void Awake()
        {
            _eadd = GetComponent<EnemyComponents>();
            
            Assert.IsNotNull(_eadd);

            _eadd.PreHurt += PreHurt;
            _eadd.PostHurt += PostHurt;
        }

        private void PreHurt(GameObject target, Vector3 force, Vector3? hitPoint, float multiplier, bool tryForExplode, float critMultiplier, GameObject sourceWeapon, bool ignoreTotalDamageTakenMultiplier, bool fromExplosion)
        {
            if (!Enabled)
            {
                return;
            }

        }

        private void PostHurt(GameObject target, Vector3 force, Vector3? hitPoint, float multiplier, bool tryForExplode, float critMultiplier, GameObject sourceWeapon, bool ignoreTotalDamageTakenMultiplier, bool fromExplosion)
        {
            if (!Enabled)
            {
                return;
            }

        }

        static FieldInfo timeControllerParryLightFi = typeof(TimeController).GetField("parryLight", BindingFlags.Instance | BindingFlags.NonPublic); 

        public bool CanParry(ProjectileBoostTracker boostTracker, double parryability)
        {
            if (!ReadyToParry)
            {
                return false;
            }

            double skill;

            if (boostTracker.NumBoosts == 0 || (boostTracker.NumEnemyBoosts == 0 && boostTracker.IsPlayerSourced))
            {
                skill = Options.FirstHitParrySkills[_enemyType].Value;
            }
            else
            {
                skill = Options.MultiHitParrySkills[_enemyType].Value;
            }

            if (skill - (1.0 - parryability) > 0.0)
            {
                return true;
            }

            return false;
        }

        public void ParryEffect(Vector3 fromPoint)
        {
            Assert.IsTrue(ReadyToParry, "EnemyFeedbacker.ParryEffect called when not ReadyToParry?");

            if (Options.HitstopOnEnemyParry.Value)
            {
                TimeScale.Controller.ParryFlash();
            }
            
            var sound = UnityEngine.Object.Instantiate((GameObject)timeControllerParryLightFi.GetValue(TimeScale.Controller), fromPoint, Quaternion.identity, transform);
            var audioSource = sound.GetComponentInChildren<AudioSource>();
            sound.GetComponentInChildren<AudioSource>().volume *= Options.EnemyParrySoundScalar.Value;
            sound.GetComponent<RemoveOnTime>().time = 0.065f;
            audioSource.SetPitch(audioSource.GetPitch() * 1.25f);
            var parryFlash = UnityEngine.Object.Instantiate(Assets.ParryFlashPrefab.ToAsset(), fromPoint, Quaternion.LookRotation((NewMovement.Instance.HeadPosition - fromPoint).normalized), Options.ParryFollowsEnemy.Value ? transform : null);
            parryFlash.transform.localScale = new Vector3(1.0f / parryFlash.transform.lossyScale.x, 1.0f / parryFlash.transform.lossyScale.y, 1.0f / parryFlash.transform.lossyScale.z);
            parryFlash.transform.localScale *= 1.5f;
            Stamina -= ParryCost;

            LastParryTimestamp.UpdateToNow();
        }

        public void ParryFinishEffect(Vector3 fromPoint)
        {
            var sound = UnityEngine.Object.Instantiate((GameObject)timeControllerParryLightFi.GetValue(TimeScale.Controller), fromPoint, Quaternion.identity, transform);
            var audioSource = sound.GetComponentInChildren<AudioSource>();
            audioSource.time = 0.05f;
            audioSource.volume *= Options.EnemyParrySoundScalar.Value * 0.5f;
            audioSource.SetPitch(audioSource.GetPitch() * 1.25f);
            sound.GetComponent<RemoveOnTime>().time = Options.EnemyParryDelay.Value;
            var parryFlash = UnityEngine.Object.Instantiate(Assets.ParryFlashPrefab.ToAsset(), fromPoint, Quaternion.LookRotation((NewMovement.Instance.HeadPosition - fromPoint).normalized), Options.ParryFollowsEnemy.Value ? transform : null);
            parryFlash.transform.localScale = new Vector3(1.0f / parryFlash.transform.lossyScale.x, 1.0f / parryFlash.transform.lossyScale.y, 1.0f / parryFlash.transform.lossyScale.z);
            parryFlash.transform.localScale *= 3.0f;
        }


        protected void Start()
        {
            _enemyType = _eadd.Eid.enemyType;

            if (_enemyType == EnemyType.V2)
            {
                var v2 = GetComponent<V2>();
                if (v2.secondEncounter)
                {
                    _enemyType = EnemyType.V2Second;
                }
            }
        }

        protected void Update()
        {
        }

        protected void FixedUpdate()
        {
            Stamina = Mathf.MoveTowards(Stamina, 1.0f, (Time.fixedDeltaTime * (float)Options.ParryStaminaRechargeRate[_enemyType].Value));

            for (int i = 0; i < _queuedParries.Count; i++)
            {
                QueuedParry queuedParry = _queuedParries[i];
                var waitTime = Options.EnemyParryDelay.Value;
                
                if (queuedParry.InitTime.TimeSince < waitTime)
                {
                    continue;
                }

                _queuedParries.RemoveAt(i);
                i -= 1;
                queuedParry.ParryAction?.Invoke(Options.ParryFollowsEnemy.Value ? (transform.position - queuedParry.PosAtTheTime) : Vector3.zero);
            }
        }

        protected void OnDestroy()
        {
        }

        public Vector3 SolveParryForce(Vector3 projectilePosition, Vector3 projectileVelocity)
        {
            var v1 = NewMovement.Instance;

            var targetPos = v1.HeadPosition;
            var targetVel = v1.travellerVelocity;

            Vector3 direction; 

            direction = (targetPos - projectilePosition).normalized;

            var targetTowardProjVel = Vector3.Project(targetVel, direction);            
            var targetTowardProjSpeed = targetTowardProjVel.magnitude * ((Vector3.Distance(targetTowardProjVel.normalized, (targetPos - projectilePosition).normalized) > 0.5f) ? -1.0f : 1.0f);
            
            var indirectTargetVel = targetVel - Vector3.Project(targetVel, direction);
            var relProjectileSpeed = projectileVelocity.magnitude + targetTowardProjSpeed;
            var dist = Vector3.Distance(projectilePosition, targetPos + (indirectTargetVel));
            var targetDirection = ((targetPos + (indirectTargetVel * (dist / relProjectileSpeed))) - projectilePosition).normalized;

            return targetDirection;
        }

        static internal void Initialize()
        {
            MonoRegistrarIndex = EnemyComponents.MonoRegistrar.Register<EnemyFeedbacker>();
        }

        private struct QueuedParry
        {
            public Action<Vector3> ParryAction;
            public FixedTimeStamp InitTime;
            public Vector3 PosAtTheTime;
        }

        private List<QueuedParry> _queuedParries = new List<QueuedParry>(2);
        private EnemyType _enemyType = EnemyType.Wicked;
    }
}