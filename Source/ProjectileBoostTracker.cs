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
            Null, RevolverBeam, RailCannon, EnemyRevolverBeam, PlayerProjectile, Projectile, HomingProjectile, Rocket, Grenade, EnemyRocket, EnemyGrenade, Coin, Nail, Saw
        }

        public bool HasBeenBoosted { get => NumPlayerBoosts != 0 || NumEnemyBoosts != 0; }
        public bool LastBoostedByPlayer = false;
        public bool IsPlayerSourced { get => ProjectileType == ProjectileCategory.RevolverBeam || ProjectileType == ProjectileCategory.PlayerProjectile || ProjectileType == ProjectileCategory.Rocket || ProjectileType == ProjectileCategory.Grenade; }
        
        public uint NumPlayerBoosts { get => _numPlayerBoosts; private set => _numPlayerBoosts = value; }
        
        public uint NumEnemyBoosts { get => _numEnemyBoosts; private set => _numEnemyBoosts = value; }
        
        public uint NumBoosts { get => NumPlayerBoosts + NumEnemyBoosts; }
        
        public ProjectileCategory ProjectileType { get => _projectileType; private set => _projectileType = value; }

        public int TimesRicocheted 
        { 
            get
            {
                if (_revBeam != null)
                {
                    return _startingRicoAmount - _revBeam.ricochetAmount;
                }

                return 0;
            }
        }

        public int CoinRicochets = 0;

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
                if (!NyxLib.Cheats.Enabled)
                {
                    return;
                }
                
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

        public bool CannonballExplodingForPlayer = false;

        public Material CustomMaterial = null;
        public Mesh CustomMesh = null;

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
                              
            if (NumEnemyBoosts == 0)
            {
                return;
            }

            _safeEnemyTypeCountDown = 0.0f;


            if (_proj != null)
            {
                if (NumBoosts == 1 && !_proj.friendly)
                {
                    _creationStartTime.UpdateToNow();
                    DebugPrintCreationStartTime();
                    _startParryabilityDist = ParryabilityTracker.NotifyCreationStart(GetHashCode());
                    _creationProgressParryabilityDist = _startParryabilityDist;
                }
            }

            IgnoreColliders = new Collider[]{};
            
            if (NumEnemyBoosts >= 1)
            {
                BoostOomph(true);
            }

            Log.Debug($"IncrementPlayerBoosts called for ProjectileBoostTracker {this}");
        }

        private void DebugPrintCreationStartTime()
        {
            Log.Debug($"{this}_creationStartTime updated, Timestamp is {_creationStartTime.TimeStamp}");
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

            TryCacheComps();

            MaybeEnforceOurExplosionPrefab();
            
            if (_proj != null)
            {
                if (bigOomph)
                {
                    Log.Debug("Oomph boosting for projectile bigly");
                    MakeExplosiveAndExplosionUnique();
                    _proj.enemyDamageMultiplier *= 2.25f;
                    _proj.damage *= 1.2f;
                    
                    if (Electric)
                    {
                        _proj.explosive = true;
                    }

                    _explosion.ExplosionScale += 0.5f;
                    _explosion.ExplosionSpeedScale += 0.5f;
                    _explosion.ExplosionDamageScale += 0.25f;
                    _explosion.ExplosionEnemyDamageMultiplierScale += 0.75f;
                    DebugPrintInfo("oomph boost big");
                }
                else
                {
                    Log.Debug("Oomph boosting for projectile not so bigly");
                    MakeExplosiveAndExplosionUnique();
                    _proj.enemyDamageMultiplier *= 1.5f;
                    _explosion.ExplosionEnemyDamageMultiplierScale += 0.75f;
                    _proj.damage *= 1.1f;
                    DebugPrintInfo("oomph boost not big");
                }
            }
            else if (_cannonball != null)
            {
                if (bigOomph)
                {
                    Log.Debug("Oomph boosting for cannonball bigly");
                    MakeExplosiveAndExplosionUnique();
                    _cannonball.damage *= 2.5f;
                    _explosion.ExplosionScale *= 1.5f;
                    _explosion.ExplosionSpeedScale *= 1.5f;
                    _explosion.ExplosionDamageScale += 0.1f;
                }
                else
                {
                    Log.Debug("Oomph boosting for not so bigly");
                    MakeExplosiveAndExplosionUnique();
                    _cannonball.damage *= 1.4f;
                }
            }

            TrySetFirstSeenDamage();
            UpdateLastSeenDamage();
        }

        private void UpdateLastSeenDamage()
        {
            if (_proj != null)
            {
                _lastSeenDamage = _proj.damage;
            }
        }

        protected void Awake()
        {
            _colliders = GetComponentsInChildren<Collider>();

            _creationProgressTime.UpdateToNow();
            if (ProjectileType == ProjectileCategory.Null)
            {
                _creationStartTime.UpdateToNow();
                DebugPrintCreationStartTime();
            }

            TryCacheComps();
        }

        protected void Start()
        {
            TrySolveType();

            _creationProgressTime.UpdateToNow();
            MaybeEnforceOurExplosionPrefab();
            UpdateLastSeenDamage();
            TrySetFirstSeenDamage();

            if (_proj != null && CustomMesh != null && CustomMaterial != null)
            {
                GetComponentInChildren<MeshFilter>().mesh = CustomMesh;
                GetComponentInChildren<MeshRenderer>().material = CustomMaterial;
            }

            StoreStartedRicochetAmount();
        }

        private void StoreStartedRicochetAmount()
        {
            if (_revBeam != null && _startingRicoAmount == -1)
            {
                _startingRicoAmount = _revBeam.ricochetAmount;
            }
        }

        private void TrySetFirstSeenDamage()
        {
            if (_firstSeenDamage.HasValue)
            {
                return;
            }

            if (_proj != null)
            {
                _firstSeenDamage = _proj.damage;
                _firstSeenEnemyMultiplier = _proj.enemyDamageMultiplier;
            }
        }

        private void TrySolveType()
        {
            if (ProjectileType == ProjectileCategory.Null)
            {
                if (!(_grenade is null))
                {
                    if (_grenade.enemy)
                    {
                        if (_grenade.rocket)
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
                        if (_grenade.rocket)
                        {
                            ProjectileType = ProjectileCategory.Rocket;
                        }
                        else
                        {
                            ProjectileType = ProjectileCategory.Grenade;
                        }
                    }
                }
                else if (!(_proj is null))
                {
                    if (_proj.playerBullet)
                    {
                        ProjectileType = ProjectileCategory.PlayerProjectile;
                    }
                    else
                    {
                        if (_proj.homingType == HomingType.None)
                        {
                            ProjectileType = ProjectileCategory.Projectile;
                        }
                        else
                        {
                            ProjectileType = ProjectileCategory.HomingProjectile;
                        }
                    }
                }
                else if (!(_revBeam is null))
                {
                    if (_revBeam.previouslyHitTransform != null && _revBeam.noMuzzleflash)
                    {
                        ProjectileType = ProjectileCategory.Coin;
                    }
                    else
                    {
                        if (_revBeam.beamType == BeamType.Enemy || _revBeam.beamType == BeamType.MaliciousFace)
                        {
                            ProjectileType = ProjectileCategory.EnemyRevolverBeam;
                        }
                        else
                        {
                            ProjectileType = ProjectileCategory.RevolverBeam;
                        }
                    }
                
                    if (Options.DifferentiateRailCannonFromBeams.Value && _revBeam.beamType == BeamType.Railgun)
                    {
                        ProjectileType = ProjectileCategory.RailCannon;
                    }

                    if (_revBeam.attributes.Contains(HitterAttribute.Electricity))
                    {
                        Electric = true;
                    }
                }
                else if (!(_coin is null))
                {
                    ProjectileType = ProjectileCategory.Coin;
                }
                else if (!(_cannonball is null))
                {
                    ProjectileType = ProjectileCategory.Rocket;
                }
                else if (!(_nail is null))
                {
                    if (_nail.chainsaw)
                    {
                        ProjectileType = ProjectileCategory.Saw;
                    }
                    else if (_nail.sawblade)
                    {
                        ProjectileType = ProjectileCategory.Saw;
                    }
                    else
                    {
                        ProjectileType = ProjectileCategory.Nail;
                    }
                }

                _startParryabilityDist = ParryabilityTracker.NotifyCreationStart(GetHashCode());
                _creationProgressParryabilityDist = _startParryabilityDist;
                _creationStartTime.UpdateToNow();
                DebugPrintCreationStartTime();
            }
            else
            {
                TryCacheComps();
            }
        }

        private bool _hasCachedComps = false;
        private void TryCacheComps()
        {
            if (_hasCachedComps)
            {
                return;
            }

            _hasCachedComps = true;
            var comps = GetComponents<MonoBehaviour>();
            
            _grenade = null;
            _proj = null;
            _revBeam = null;
            _coin = null;
            _cannonball = null;
            _nail = null;

            foreach (var comp in comps)
            {
                switch (comp)
                {
                    case Grenade castedComp:
                        _grenade = castedComp;
                        break;
                    case Projectile castedComp:
                        _proj = castedComp;
                        break;
                    case RevolverBeam castedComp:
                        _revBeam = castedComp;
                        break;
                    case Coin castedComp:
                        _coin = castedComp;
                        break;
                    case Cannonball castedComp:
                        _cannonball = castedComp;
                        break;
                    case Nail castedComp:
                        _nail = castedComp;
                        break;
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
            if (!NyxLib.Cheats.Enabled)
            {
                return;
            }

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

            if (_grenade != null && NumEnemyBoosts >= 1)
            {
                if (_grenade.playerRiding)
                {
                    IgnoreColliders = null;
                }
            }

            MaybeEnforceOurExplosionPrefab();
        }

        private void MaybeEnforceOurExplosionPrefab()
        {
            if (!NyxLib.Cheats.Enabled)
            {
                return;
            }

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

            //Log.Message($"{CoinRicochets}");
            
            TrySolveType();

            var contactDiffDist = ParryabilityTracker.NotifyContact(GetHashCode());

            if (_creationStartTime.TimeStamp <= 0.001)
            {
                _creationStartTime.UpdateToNow();
                Log.Debug($"{name}: Creation start time had to be updated by NotifyContact");
            }

            var options = Options.GetOptionsForType(ProjectileType);

            double window = Math.Max(Math.Max(0.4 + (_creationStartTime.TimeSince * 0.25), 0.3 + (_creationProgressTime.TimeSince * 0.5)), options.MinimumParryWindow.Value);

            double creationDist = _creationProgressParryabilityDist;

            if (_startParryabilityDist < _creationProgressParryabilityDist)
            {
                double startParryabilityDistWeight = 1.0f / (NumBoosts + 1);
                
                creationDist = ((_startParryabilityDist * startParryabilityDistWeight) + _creationProgressParryabilityDist) / (1.0 + (1.0 * startParryabilityDistWeight));
            }
            
            var diffDist = Math.Min(contactDiffDist, creationDist);

            var parryability = Mathf.Clamp01(NyxMath.InverseNormalizeToRange((float)diffDist, (float)window / 2, (float)window));

            Log.Debug($"ProjectileBoostTracker.NotifyContact called and is giving a window of {window}, a diffDist of {diffDist} and a contactDiffDist of {contactDiffDist}, and a creationDist of {creationDist} (start: {_startParryabilityDist}, progress: {_creationProgressParryabilityDist}), resulting in a parryability of {parryability} (hash: {GetHashCode()})");
            
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

            byte flagsByte = 0;
            if (Options.DifferentiateElectricity.Value)
            {
                if (Electric)
                {
                    flagsByte ^= 0b10000000;
                }
            }

            int countedCoinRicos = 0;

            if (Options.DifferentiateCoinRicochets.Value)
            {
                countedCoinRicos = CoinRicochets / Options.CoinRicochetDivisor.Value;
            }

            return BitConverter.ToInt32(new byte[] { boostByte, (byte)ProjectileType, (byte)(TimesRicocheted + countedCoinRicos), flagsByte}, 0);
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
            _startingRicoAmount = other._startingRicoAmount;
            Electric = other.Electric;
            CustomMesh = other.CustomMesh;
            CustomMaterial = other.CustomMaterial;
            CoinRicochets = other.CoinRicochets;

            if (other._explosiveAndExplosionUnique && NyxLib.Cheats.Enabled)
            {
                _prefabHolder = new GameObject();
                _prefabHolder.transform.parent = transform;
                _prefabHolder.SetActive(false);
                _explosion = GameObject.Instantiate(_explosion.gameObject, _prefabHolder.transform).GetComponent<ExplosionAdditions>();
                _explosiveAndExplosionUnique = true;
            }

            var proj = GetComponent<Projectile>();

            if (other._proj != null && proj != null && NyxLib.Cheats.Enabled)
            {
                var explosion = other._proj.explosionEffect.GetComponentInChildren<Explosion>();
                
                if (explosion != null)
                {
                    proj.explosionEffect = other._proj.explosionEffect;
                }
            }

            var revolverBeam = other.GetComponent<RevolverBeam>();

            if (revolverBeam != null && proj != null && NyxLib.Cheats.Enabled)
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
            if (!NyxLib.Cheats.Enabled)
            {
                return;
            }

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
            if (!NyxLib.Cheats.Enabled)
            {
                return;
            }

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
                _explosion.BaseDamageOverride = _firstSeenDamage;
                _explosion.ExplosionEnemyDamageMultiplierScale *= _firstSeenEnemyMultiplier.GetValueOrDefault(1.0f);
            }
        }

        internal void DebugPrintInfo(string reason = "no reason specified")
        {
            string printStr = $"{name} Debug Info! (because {reason}):";
            if (_proj != null)
            {
                var expadd = _proj.explosionEffect.GetComponentInChildren<ExplosionAdditions>();
                printStr += $"\nactive type: projectile";
                printStr += $"\noverall type: {ProjectileType}";
                printStr += $"\nprojDamage: {_proj.damage}";
                printStr += $"\n_firstSeenDamage: {_firstSeenDamage}";
                printStr += $"\nprojEnemyDamageMult: {_proj.enemyDamageMultiplier}";
                printStr += $"\nexplosionBaseDamageOverride: {_explosion?.BaseDamageOverride}";
                printStr += $"\nexplosionDamageScale: {_explosion?.ExplosionDamageScale}";
                printStr += $"\nExplosionEnemyDamageMultiplierScale: {_explosion?.ExplosionEnemyDamageMultiplierScale}";
                printStr += $"\n_explosion is _proj.explosionEffect:[ExplosionAdditionsComp]: {_explosion == expadd}";
                printStr += $"\n_explosion is null: {_explosion is null}";
                printStr += $"\n_proj.explosionEffect:[ExplosionAdditionsComp] is null: {expadd is null}";
            }
            else if (_revBeam != null)
            {
                printStr += $"\nactive type: revolver beam";
                printStr += $"\noverall type: {ProjectileType}";
                printStr += $"\nTimesRicocheted: {TimesRicocheted}";
                printStr += $"\nstarting ricochet amount: {_startingRicoAmount}";
            }
            else
            {
                printStr += $"\nno specific info setup :c";
            }

            printStr += $"\ntimeSinceCreation: {_creationStartTime.TimeSince}";
            printStr += $"\ncreationTimestamp: {_creationStartTime.TimeStamp}";
            printStr += $"\ntimeSinceCreationProgress: {_creationProgressTime.TimeSince}";
            printStr += $"\ncreationProgressTimestamp: {_creationProgressTime.TimeStamp}";

            Log.Debug(printStr);
        }

        [SerializeField] private uint _numPlayerBoosts = 0;
        [SerializeField] private uint _numEnemyBoosts = 0;
        [SerializeField] private ProjectileCategory _projectileType = ProjectileCategory.Null;

        private static FieldInfo _cballInterruptionExplosionFi = AccessTools.Field(typeof(Cannonball), "interruptionExplosion");
        [SerializeField] private double _startParryabilityDist = double.PositiveInfinity;
        [SerializeField] private double _creationProgressParryabilityDist = double.PositiveInfinity;
        [SerializeField] private FixedTimeStamp _creationStartTime;
        [SerializeField] private FixedTimeStamp _creationProgressTime;
        private Collider[] _ignoreColliders = new Collider[0];
        private Collider[] _colliders = null;
        private Grenade _grenade = null;
        private Projectile _proj = null;
        private Coin _coin = null;
        private double _safeEnemyTypeCountDown = -1.0;
        private bool _explosiveAndExplosionUnique = false;
        private ExplosionAdditions _explosion = null;
        private GameObject _prefabHolder = null;
        [SerializeField] private bool _canBeEnemyParried = true;
        private Nail _nail;
        private float? _firstSeenDamage = null;
        private float? _firstSeenEnemyMultiplier = null;
        private float _lastSeenDamage = 0.0f;
        private RevolverBeam _revBeam;
        [SerializeField] private int _startingRicoAmount = -1;
    }
}