using System;
using System.Collections.Generic;
using System.Linq;
using Nyxpiri.ULTRAKILL.NyxLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nyxpiri.ULTRAKILL.FeedbackersForEveryone
{
    public static class ParryabilityTracker
    {
        public static void Initialize()
        {
            ScenesEvents.OnSceneWasLoaded += OnSceneLoad;
            UpdateEvents.OnFixedUpdate += FixedUpdate;
        }

        private static void FixedUpdate()
        {
            for (int i = 0; i < _parryabilitys.Count; i++)
            {
                _parryabilitys[i].FixedUpdate();
            }
        }

        public static double NotifyContact(int hash)
        {
            ParryabilityInfo parryability = GetOrMakeParryability(hash);

            Log.Debug($"ParryabilityTracker.NotifyContact called with hash {hash}");

            return parryability.NotifyContact();
        }

        public static double NotifyCreationProgress(int hash)
        {
            ParryabilityInfo parryability = GetOrMakeParryability(hash);

            Log.Debug($"ParryabilityTracker.NotifyCreationProgress called with hash {hash}");

            return parryability.NotifyCreationProgress();
        }

        public static double NotifyCreationStart(int hash)
        {
            ParryabilityInfo parryability = GetOrMakeParryability(hash);

            Log.Debug($"ParryabilityTracker.NotifyCreationStart called with hash {hash}");

            return parryability.NotifyCreationStart();
        }

        private static ParryabilityInfo GetOrMakeParryability(int hash)
        {
            if (!_attackParryabilitysDict.TryGetValue(hash, out ParryabilityInfo parryability))
            {
                parryability = new ParryabilityInfo($"{hash}");
                _attackParryabilitysDict.Add(hash, parryability);
                _parryabilitys.Add(parryability);
            }

            return parryability;
        }

        private static void OnSceneLoad(Scene sceneIdx, string levelName, string unitySceneName)
        {
            _attackParryabilitysDict.Clear();
            _parryabilitys.Clear();
        }

        private static Dictionary<int, ParryabilityInfo> _attackParryabilitysDict = new Dictionary<int, ParryabilityInfo>(256);
        private static List<ParryabilityInfo> _parryabilitys = new List<ParryabilityInfo>(256);

        private class ParryabilityInfo
        {
            public static int QueueCap { get => Options.ParryabilityMemory.Value; }

            public double NotifyContact()
            {
                _contactTimestamps.EnqueueNew();
                Log.Debug($"{_debugName}:ParryabilityTracker.ParryabilityInfo.NotifyContact called and is giving a bestDiffDist of {_contactTimestamps.BestDiffDist}");
                return _contactTimestamps.BestDiffDist;
            }

            public double NotifyCreationProgress()
            {
                _creationProgressTimestamps.EnqueueNew();
                Log.Debug($"{_debugName}:ParryabilityTracker.ParryabilityInfo.NotifyCreationProgress called and is giving a bestDiffDist of {_creationProgressTimestamps.BestDiffDist}");
                return _creationProgressTimestamps.BestDiffDist;
            }

            public double NotifyCreationStart()
            {
                _creationStartTimestamps.EnqueueNew();
                Log.Debug($"{_debugName}:ParryabilityTracker.ParryabilityInfo.NotifyCreationStart called and is giving a bestDiffDist of {_creationStartTimestamps.BestDiffDist}");
                return _creationStartTimestamps.BestDiffDist;
            }

            internal void FixedUpdate()
            {
                _creationStartTimestamps.FixedUpdate();
                _creationProgressTimestamps.FixedUpdate();
                _contactTimestamps.FixedUpdate();
            }

            internal ParryabilityInfo(string debugName) 
            {
                _debugName = debugName;
                _creationStartTimestamps = new TimestampsQueue();
                _creationStartTimestamps.Init($"{_debugName}.creationStartTimestamps");
                _creationProgressTimestamps = new TimestampsQueue();
                _creationProgressTimestamps.Init($"{_debugName}.creationProgressTimestamps");
                _contactTimestamps = new TimestampsQueue();
                _contactTimestamps.Init($"{_debugName}.contactTimestamps");
            }

            private string _debugName;
            private TimestampsQueue _creationStartTimestamps;
            private TimestampsQueue _creationProgressTimestamps;
            private TimestampsQueue _contactTimestamps;

            private struct TimestampsQueue
            {
                public float BestDiffDist { get; private set; }
                public static double MaxDecayTime { get => 20.0; }

                internal bool CanPeek
                { 
                    get
                    {
                        return _queue.Count > 0;
                    }
                }

                /* 
                not sure if there's a better way for C# (on this version, newer versions have the feature) but parameterless
                constructors seems to be unsupported for structs so I'm doing this pattern which is admittedly yuck
                */
                internal void Init(string debugName) 
                {
                    _debugName = debugName;
                    _queue = new Queue<FixedTimeStamp>(QueueCap);
                    _decayTimestamp.UpdateToNow();
                    _averageDiffs = new List<double>(QueueCap - 1);
                    UpdateDecayTime();
                    UpdateBestDiffDist();
                }

                internal FixedTimeStamp Peek()
                {
                    return _queue.Peek();
                }

                internal void EnqueueNew()
                {
                    if (_LastEnqueueTimestamp.TimeSince < 0.25f)
                    {
                        return;
                    }
                    
                    if (_queue.Count == QueueCap)
                    {
                        _queue.Dequeue();
                    }

                    FixedTimeStamp timestamp = new FixedTimeStamp();
                    timestamp.UpdateToNow();
                    _queue.Enqueue(timestamp);
                    _decayTimestamp.UpdateToNow();
                    UpdateDecayTime();
                    UpdateBestDiffDist();
                    _LastEnqueueTimestamp.UpdateToNow();
                }

                private void UpdateBestDiffDist()
                {
                    if (_queue.Count <= 2)
                    {
                        BestDiffDist = 10000.0f;
                        Log.Debug($"{_debugName}:ParryabilityTracker.ParryabilityInfo.TimestampsQueue.UpdateBestDiffDist ended with a BestDiffDist of {BestDiffDist} (based on queue being small)");
                        return;
                    }

                    double? firstDiff = null;
                    double lastDiff = 0.0;
                    double sum = 0.0;
                    _averageDiffs.Clear();
                    FixedTimeStamp? lastTimestamp = null;
                    BestDiffDist = 0.0f;

                    foreach (var timestamp in _queue.Reverse())
                    {
                        if (!lastTimestamp.HasValue)
                        {
                            lastTimestamp = timestamp;
                            continue;
                        }
                 
                        lastDiff = (lastTimestamp.Value.TimeStamp - timestamp.TimeStamp);

                        firstDiff ??= lastDiff;   

                        sum += lastDiff;
                        _averageDiffs.Add(sum);
                        lastTimestamp = timestamp;
                    }

                    double lowestDist = double.PositiveInfinity;
                    int lowestDistI = -1;

                    for (int i = 1; i < _averageDiffs.Count; i++)
                    {
                        _averageDiffs[i] /= i + 1;
                        var diff = _averageDiffs[i];
                        var diffDist = Math.Abs(diff - firstDiff.Value);

                        if (diffDist < lowestDist)
                        {
                            lowestDist = diffDist;
                            lowestDistI = i;
                        }
                    }

                    BestDiffDist = (float)lowestDist;
                    Log.Debug($"{_debugName}:ParryabilityTracker.ParryabilityInfo.TimestampsQueue.UpdateBestDiffDist ended with a BestDiffDist of {BestDiffDist}, and a lowestDistI of {lowestDistI}. _queue.Count: {_queue.Count}");
                }

                internal void FixedUpdate()
                {
                    if (_queue.Count == 0)
                    {
                        return;
                    }
                    
                    if (_decayTimestamp.TimeSince > _decayTime)
                    {
                        Decay();
                    }
                }

                private void Decay()
                {
                    if (_queue.Count == 0)
                    {
                        return;
                    }

                    _decayTimestamp.UpdateToNow();
                    _queue.Dequeue();
                    UpdateBestDiffDist();
                }

                private void UpdateDecayTime()
                {
                    _decayTime = 0.0f;

                    if (_queue.Count <= 2)
                    {
                        _decayTime = (float)MaxDecayTime;
                        return;
                    }

                    FixedTimeStamp? lastTimestamp = null;
                    foreach (var timestamp in _queue)
                    {
                        if (!lastTimestamp.HasValue)
                        {
                            lastTimestamp = timestamp;
                            continue;
                        }

                        _decayTime += (float)(timestamp.TimeStamp - lastTimestamp.Value.TimeStamp);
                        lastTimestamp = timestamp;
                    }

                    _decayTime /= _queue.Count - 1;
                    _decayTime *= 2.0f;
                    _decayTime = Mathf.Clamp(_decayTime, 1.0f, (float)MaxDecayTime);
                    Log.Debug($"{_debugName}:ParryabilityTracker.ParryabilityInfo.TimestampsQueue.UpdateDecayTime ended with a _decayTime of {_decayTime}, _queue.Count: {_queue.Count}");
                }

                private string _debugName;
                private List<double> _averageDiffs;
                private Queue<FixedTimeStamp> _queue;
                private FixedTimeStamp _decayTimestamp;
                private FixedTimeStamp _LastEnqueueTimestamp;
                private float _decayTime;
            }
        }
    }
}