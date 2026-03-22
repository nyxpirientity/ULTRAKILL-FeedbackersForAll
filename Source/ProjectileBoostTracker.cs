using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nyxpiri.ULTRAKILL.NyxLib;
using UnityEngine;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    public class ProjectileBoostTracker : MonoBehaviour
    {
        public enum ProjectileCategory : byte
        {
            Null, RevolverBeam, EnemyRevolverBeam, PlayerProjectile, Projectile, HomingProjectile, Rocket, Grenade, EnemyRocket, EnemyGrenade, Coin, Nail, Saw
        }

        public bool HasBeenBoosted { get => NumPlayerBoosts != 0 || NumEnemyBoosts != 0; }
        public bool LastBoostedByPlayer = false;
        
        public uint NumPlayerBoosts { get => _numPlayerBoosts; private set => _numPlayerBoosts = value; }
        
        public uint NumEnemyBoosts { get => _numEnemyBoosts; private set => _numEnemyBoosts = value; }
        
        public uint NumBoosts { get => NumPlayerBoosts + NumEnemyBoosts; }
        
        public ProjectileCategory ProjectileType { get => _projectileType; private set => _projectileType = value; }

        private Cannonball _cannonball;
        public EnemyIdentifier SafeEid = null;
        public bool Electric = false;
        public bool CoinPunched = false;

        public IReadOnlyList<Collider> Colliders
        {
            get => _colliders;
        }
        public IReadOnlyList<Collider> IgnoreColliders
        {
            get => _ignoreColliders;
            set
            {
                foreach (var otherCol in _ignoreColliders)
                {
                    if (otherCol == null)
                    {
                        continue;
                    }

                    foreach (var col in _colliders)
                    {
                        Physics.IgnoreCollision(otherCol, col, false);
                    }
                }
                
                if (value == null)
                {
                    _ignoreColliders = new Collider[0];
                    return;
                }
                
                _ignoreColliders = value.ToArray();

                foreach (var otherCol in _ignoreColliders)
                {
                    if (otherCol == null)
                    {
                        continue;
                    }
                    foreach (var col in _colliders)
                    {
                        Physics.IgnoreCollision(otherCol, col, true);
                    }
                }
            }
        }

        public void IncrementPlayerBoosts()
        {
            if (LastBoostedByPlayer && NumPlayerBoosts > 0)
            {
                return;
            }

            NumPlayerBoosts += 1;
            
            LastBoostedByPlayer = true;
            _canBeEnemyParried = true;

            _creationProgressParryabilityDist = ParryabilityTracker.NotifyCreationProgress(GetHashCode());
            _creationProgressTime.UpdateToNow();

            SafeEid = null;
            _safeEnemyTypeCountDown = 0.0f;
            
            if (NumEnemyBoosts == 0)
            {
                return;
            }

            if (_proj != null)
            {
                if (NumBoosts == 1 && !_proj.friendly)
                {
                    _creationStartTime.UpdateToNow();
                    _startParryabilityDist = ParryabilityTracker.NotifyCreationStart(GetHashCode());
                }
            }

            IgnoreColliders = new Collider[]{};
            
            if (NumPlayerBoosts > 1 || NumEnemyBoosts >= 1)
            {
                BoostOomph(true);
            }

            Log.Debug($"IncrementPlayerBoosts called for ProjectileBoostTracker {this}");
        }

        public void IncrementEnemyBoost()
        {
            if (!LastBoostedByPlayer && NumEnemyBoosts > 0)
            {
                return;
            }

            if (NumEnemyBoosts == 0)
            {
                for (int i = 1; i < NumPlayerBoosts; i++)
                {
                    BoostOomph(true);
                }
            }

            NumEnemyBoosts += 1;
            LastBoostedByPlayer = false;
            _creationProgressParryabilityDist = ParryabilityTracker.NotifyCreationProgress(GetHashCode());
            _creationProgressTime.UpdateToNow();
            
            _canBeEnemyParried = true;
            BoostOomph(false);
            Log.Debug($"IncrementEnemyBoosts called for ProjectileBoostTracker {this}");
        }

        private void BoostOomph(bool bigOomph)
        {
            if (!NyxLib.Cheats.Enabled)
            {
                return;
            }

            if (NumEnemyBoosts == 0)
            {
                return;
            }
            
            if (_proj != null)
            {
                if (bigOomph)
                {
                    Log.Debug("Oomph boosting for projectile bigly");
                    MakeExplosiveAndExplosionUnique();
                    _proj.enemyDamageMultiplier *= 2.0f;
                    _proj.damage *= 1.2f;
                    _explosion.ExplosionScale += 0.5f;
                    _explosion.ExplosionSpeedScale += 0.5f;
                    _explosion.ExplosionDamageScale += 0.25f;
                    _explosion.ExplosionEnemyDamageMultiplierScale += 0.75f;
                }
                else
                {
                    Log.Debug("Oomph boosting for projectile not so bigly");
                    MakeExplosiveAndExplosionUnique();
                    _proj.enemyDamageMultiplier *= 1.2f;
                    _proj.damage *= 1.1f;
                }
            }
            else if (_cannonball != null)
            {
                if (bigOomph)
                {
                    Log.Debug("Oomph boosting for cannonball bigly");
                    MakeExplosiveAndExplosionUnique();
                    _cannonball.damage *= 2.0f;
                    _explosion.ExplosionScale *= 1.5f;
                    _explosion.ExplosionSpeedScale *= 1.5f;
                    _explosion.ExplosionDamageScale += 0.1f;
                }
                else
                {
                    Log.Debug("Oomph boosting for not so bigly");
                    MakeExplosiveAndExplosionUnique();
                    _cannonball.damage *= 1.1f;
                }
            }
        }

        protected void Awake()
        {
            _colliders = GetComponentsInChildren<Collider>();

            if (ProjectileType == ProjectileCategory.Null)
            {
                _creationStartTime.UpdateToNow();
                _creationProgressTime.UpdateToNow();
            }

            // revolver beam changes this
            /*try 
            {
                Assert.IsNotNull(_colliders, "Projectile without a collider? Didn't know it existed!");
                Assert.IsFalse(_colliders.Length == 0, "Projectile without a collider? Didn't know it existed!");
            }
            catch (System.Exception e)
            {
                Log.Error($"Soft Error in ProjectileBoostTracker.Awake: {e}"); // don't want to block projectile awake and stuff if this fails
            } */
        }

        protected void Start()
        {
            TrySolveType();

            _creationProgressTime.UpdateToNow();
            MaybeEnforceOurExplosionPrefab();
        }

        private void TrySolveType()
        {
            if (ProjectileType == ProjectileCategory.Null)
            {
                if (TryGetComponent(out Grenade grenade))
                {
                    if (grenade.enemy)
                    {
                        if (grenade.rocket)
                        {
                            ProjectileType = ProjectileCategory.EnemyRocket;
                        }
                        else
                        {
                            ProjectileType = ProjectileCategory.EnemyGrenade;
                        }
                    }
                    else
                    {
                        if (grenade.rocket)
                        {
                            ProjectileType = ProjectileCategory.Rocket;
                        }
                        else
                        {
                            ProjectileType = ProjectileCategory.Grenade;
                        }
                    }
                }
                else if (TryGetComponent(out Projectile proj))
                {
                    _proj = proj;

                    if (proj.playerBullet)
                    {
                        ProjectileType = ProjectileCategory.PlayerProjectile;
                    }
                    else
                    {
                        if (proj.homingType == HomingType.None)
                        {
                            ProjectileType = ProjectileCategory.Projectile;
                        }
                        else
                        {
                            ProjectileType = ProjectileCategory.HomingProjectile;
                        }
                    }
                }
                else if (TryGetComponent(out RevolverBeam revolverBeam))
                {
                    if (revolverBeam.previouslyHitTransform != null && revolverBeam.noMuzzleflash)
                    {
                        ProjectileType = ProjectileCategory.Coin;
                    }
                    else
                    {
                        if (revolverBeam.beamType == BeamType.Enemy || revolverBeam.beamType == BeamType.MaliciousFace)
                        {
                            ProjectileType = ProjectileCategory.EnemyRevolverBeam;
                        }
                        else
                        {
                            ProjectileType = ProjectileCategory.RevolverBeam;
                        }
                    }

                    if (revolverBeam.attributes.Contains(HitterAttribute.Electricity))
                    {
                        Electric = true;
                    }
                }
                else if (TryGetComponent(out Coin coin))
                {
                    ProjectileType = ProjectileCategory.Coin;
                }
                else if (TryGetComponent(out Cannonball cannonball))
                {
                    ProjectileType = ProjectileCategory.Rocket;
                    _cannonball = cannonball;
                }
                else if (TryGetComponent(out Nail nail))
                {
                    if (nail.chainsaw)
                    {
                        ProjectileType = ProjectileCategory.Saw;
                    }
                    else if (nail.sawblade)
                    {
                        ProjectileType = ProjectileCategory.Saw;
                    }
                    else
                    {
                        ProjectileType = ProjectileCategory.Nail;
                    }

                    _nail = nail;
                }


                _startParryabilityDist = ParryabilityTracker.NotifyCreationStart(GetHashCode());
                _creationStartTime.UpdateToNow();
            }
            else
            {
                if (TryGetComponent(out Cannonball cannonball))
                {
                    _cannonball = cannonball;
                }
                else if (TryGetComponent(out Projectile proj))
                {
                    _proj = proj;
                }
            }
        }

        internal void PreNailFixedUpdate()
        {
            if (_nail == null)
            {
                _nail = GetComponent<Nail>();
            }

            if (_nail.punched)
            {
                IncrementPlayerBoosts();
            }
        }

        protected void FixedUpdate()
        {
            if (_safeEnemyTypeCountDown >= 0.0)
            {
                _safeEnemyTypeCountDown -= Time.fixedDeltaTime;

                if (_safeEnemyTypeCountDown <= 0.0)
                {
                    _safeEnemyTypeCountDown = -1.0;
                    if (_proj != null)
                    {
                        _proj.safeEnemyType = EnemyType.Idol;
                    }
                }
            }

            MaybeEnforceOurExplosionPrefab();
        }

        private void MaybeEnforceOurExplosionPrefab()
        {
            if (_explosiveAndExplosionUnique)
            {
                if (_proj != null)
                {
                    _proj.explosionEffect = _explosion.gameObject;
                }
                else if (_cannonball != null)
                {
                    _cballInterruptionExplosionFi.SetValue(_cannonball, _explosion.gameObject);
                }
            }
        }

        public double NotifyContact()
        {
            if (!_canBeEnemyParried)
            {
                return 0.0;
            }
            
            TrySolveType();

            var contactDiffDist = ParryabilityTracker.NotifyContact(GetHashCode());

            double window = Math.Max(Math.Max(0.4 + (_creationStartTime.TimeSince * 0.25), 0.3 + (_creationProgressTime.TimeSince * 0.5)), 0.75);

            double creationDist = _creationProgressParryabilityDist;

            if (_startParryabilityDist < _creationProgressParryabilityDist)
            {
                double startParryabilityDistWeight = 1.0f / (NumBoosts + 1);
                
                creationDist = ((_startParryabilityDist * startParryabilityDistWeight) + _creationProgressParryabilityDist) / (1.0 + (1.0 * startParryabilityDistWeight));
            }
            
            var diffDist = Math.Min(contactDiffDist, creationDist);

            var parryability = Mathf.Clamp01(NyxMath.InverseNormalizeToRange((float)diffDist, (float)window / 2, (float)window));

            Log.Debug($"ProjectileBoostTracker.NotifyContact called and is giving a window of {window}, a diffDist of {diffDist} and a contactDiffDist of {contactDiffDist}, resulting in a parryability of {parryability} (hash: {GetHashCode()})");
            
            return parryability;
        }

        public override int GetHashCode()
        {
            byte playerBoostByte = ((byte)(Math.Min(NumPlayerBoosts, 15)));
            byte enemyBoostByte = (byte)Math.Min(NumEnemyBoosts, 15);

            if (BitConverter.IsLittleEndian)
            {
                enemyBoostByte <<= 4;
            }
            else
            {
                enemyBoostByte >>= 4;
            }

            byte boostByte = (byte)(playerBoostByte ^ enemyBoostByte);

            //MelonLogger.Msg($"TEST PRINT FOR ProjectileBoostTracker.GetHashCode\nplayerBoostByte:{Convert.ToString(playerBoostByte, toBase: 2)}\nenemyBoostByte:{Convert.ToString(enemyBoostByte, toBase: 2)}\nboostByte:{Convert.ToString(boostByte, toBase: 2)}");
            return BitConverter.ToInt32(new byte[] { boostByte, (byte)ProjectileType, 0, 0}, 0);
        }

        public void CopyFrom(ProjectileBoostTracker other)
        {
            ProjectileType = other.ProjectileType;
            NumPlayerBoosts = other.NumPlayerBoosts;
            NumEnemyBoosts = other.NumEnemyBoosts;
            LastBoostedByPlayer = other.LastBoostedByPlayer;
            _startParryabilityDist = other._startParryabilityDist;
            _creationProgressTime = other._creationProgressTime;
            _creationStartTime = other._creationStartTime;
            Electric = other.Electric;

            if (other._explosiveAndExplosionUnique)
            {
                _prefabHolder = new GameObject();
                _prefabHolder.transform.parent = transform;
                _prefabHolder.SetActive(false);
                _explosion = GameObject.Instantiate(_explosion.gameObject, _prefabHolder.transform).GetComponent<ExplosionAdditions>();
                _explosiveAndExplosionUnique = true;
            }

            var proj = GetComponent<Projectile>();

            if (other._proj != null && proj != null)
            {
                var explosion = other._proj.explosionEffect.GetComponentInChildren<Explosion>();
                
                if (explosion != null)
                {
                    proj.explosionEffect = other._proj.explosionEffect;
                }
            }

            var revolverBeam = other.GetComponent<RevolverBeam>();

            if (revolverBeam != null && proj != null)
            {
                var explosion = revolverBeam.hitParticle.GetComponentInChildren<Explosion>();
                
                if (explosion != null)
                {
                    _prefabHolder ??= new GameObject();
                    _prefabHolder.SetActive(false);
                    _prefabHolder.transform.parent = transform;
                    proj.explosionEffect = GameObject.Instantiate(revolverBeam.hitParticle, _prefabHolder.transform);
                    _explosion = proj.explosionEffect.GetOrAddComponent<ExplosionAdditions>();
                    _explosiveAndExplosionUnique = true;
                }
            }
        }

        internal void SetTempSafeEnemyType(EnemyType enemyType)
        {
            if (_proj == null)
            {
                return;
            }

            _proj.safeEnemyType = enemyType;
            _safeEnemyTypeCountDown = 0.1;
        }

        internal void MarkCannotBeEnemyParried()
        {
            _canBeEnemyParried = false;
        }

        private void MakeExplosiveAndExplosionUnique()
        {
            if (_explosiveAndExplosionUnique)
            {
                return;
            }

            Log.Debug($"Making explosive and explosion unique for {this}");

            _prefabHolder ??= new GameObject();
            _prefabHolder.transform.parent = transform;
            _prefabHolder.SetActive(false);

            if (_proj != null)
            {
                var explosion = _proj.explosionEffect.GetComponentsInChildren<Explosion>();

                if (explosion != null)
                {
                    _proj.explosionEffect = GameObject.Instantiate(_proj.explosionEffect, _prefabHolder.transform);
                    _explosion = _proj.explosionEffect.GetOrAddComponent<ExplosionAdditions>();
                }
                else
                {
                    _proj.explosionEffect = GameObject.Instantiate(NyxLib.Assets.ExplosionPrefab, _prefabHolder.transform);
                    _explosion = _proj.explosionEffect.GetComponent<ExplosionAdditions>();
                }
            }
            else if (_cannonball != null)
            {
                var interruptionExplosion = _cballInterruptionExplosionFi.GetValue(_cannonball) as GameObject;

                if (interruptionExplosion != null)
                {
                    _cballInterruptionExplosionFi.SetValue(_cannonball, GameObject.Instantiate(interruptionExplosion, _prefabHolder.transform));
                    interruptionExplosion = _cballInterruptionExplosionFi.GetValue(_cannonball) as GameObject;
                    _explosion = interruptionExplosion.GetOrAddComponent<ExplosionAdditions>();
                }
                else
                {
                    _cballInterruptionExplosionFi.SetValue(_cannonball, GameObject.Instantiate(NyxLib.Assets.ExplosionPrefab, _prefabHolder.transform));
                    interruptionExplosion = _cballInterruptionExplosionFi.GetValue(_cannonball) as GameObject;
                    _explosion = interruptionExplosion.GetComponent<ExplosionAdditions>();
                }
            }

            if (_explosion != null)
            {
                _explosion.ForceElectric = Electric;
            }
        }

        [SerializeField] private uint _numPlayerBoosts = 0;
        [SerializeField] private uint _numEnemyBoosts = 0;
        [SerializeField] private ProjectileCategory _projectileType = ProjectileCategory.Null;

        private static FieldInfo _cballInterruptionExplosionFi = AccessTools.Field(typeof(Cannonball), "interruptionExplosion");
        [SerializeField] private double _startParryabilityDist = double.PositiveInfinity;
        [SerializeField] private double _creationProgressParryabilityDist = double.PositiveInfinity;
        private FixedTimeStamp _creationStartTime = new FixedTimeStamp();
        private FixedTimeStamp _creationProgressTime = new FixedTimeStamp();
        private Collider[] _ignoreColliders = new Collider[0];
        private Collider[] _colliders = null;
        private Projectile _proj;
        private double _safeEnemyTypeCountDown = -1.0;
        private bool _explosiveAndExplosionUnique = false;
        private ExplosionAdditions _explosion = null;
        private GameObject _prefabHolder = null;
        [SerializeField] private bool _canBeEnemyParried = true;
        private Nail _nail;
    }
}