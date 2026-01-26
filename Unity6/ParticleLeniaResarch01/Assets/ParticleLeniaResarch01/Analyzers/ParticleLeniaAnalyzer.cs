using Assets.ParticleLeniaResarch01.Analyzers;
using Assets.ParticleLeniaResarch01.Scripts.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine.tvOS;
using static Assets.ParticleLeniaResarch01.Analyzers.ParticleLeniaAnalyzerTiming;
using static UnityEngine.InputSystem.HID.HID;

namespace Assets.ParticleLeniaResarch01.Scripts
{
    public class ParticleLeniaAnalyzer : IEnumerable<IParticleLeniaAnalyzer>, IReadOnlyList<IParticleLeniaAnalyzer>
    {
        private class ParticleLeniaAnalyzerEvents
        {
            private ParticleLeniaAnalyzerEvent[] loopStartArray;
            private ParticleLeniaAnalyzerEvent[] loopEndArray;
            private ParticleLeniaAnalyzerForeachEvent[] foreachArray;

            private readonly List<ParticleLeniaAnalyzerEvent> loopStartList = new();
            private readonly List<ParticleLeniaAnalyzerEvent> loopEndList = new();
            private readonly List<ParticleLeniaAnalyzerForeachEvent> foreachList = new();

            private readonly ParticleLeniaAnalyzerTiming timing;

            public ParticleLeniaAnalyzerEvents(ParticleLeniaAnalyzerTiming timing)
            {
                loopStartArray = Array.Empty<ParticleLeniaAnalyzerEvent>();
                loopEndArray = Array.Empty<ParticleLeniaAnalyzerEvent>();
                foreachArray = Array.Empty<ParticleLeniaAnalyzerForeachEvent>();
                this.timing = timing;
            }

            public bool ForeachEnabled => foreachArray != null && foreachArray.Length > 0;

            public void MakeArray()
            {
                loopStartArray = loopStartList.ToArray();
                loopEndArray = loopEndList.ToArray();
                foreachArray = foreachList.ToArray();
            }

            public void Clear()
            {
                loopStartList.Clear();
                loopEndList.Clear();
                foreachList.Clear();
                loopStartArray = Array.Empty<ParticleLeniaAnalyzerEvent>();
                loopEndArray = Array.Empty<ParticleLeniaAnalyzerEvent>();
                foreachArray = Array.Empty<ParticleLeniaAnalyzerForeachEvent>();
            }

            public void Add(IParticleLeniaAnalyzer particleLeniaAnalyzer)
            {
                particleLeniaAnalyzer.AppendTo(timing, loopStartList, loopEndList, foreachList);
            }

            public void LoopStart(ParticleInfoArray particles)
            {
                foreach (var item in loopStartArray.AsSpan())
                {
                    item(particles);
                }
            }

            public void LoopEnd(ParticleInfoArray particles)
            {
                Parallel.ForEach(loopEndArray, item =>
                {
                    item(particles);
                });
            }

            private void Foreach(ParticleInfo info, int index)
            {
                foreach (var item in foreachArray.AsSpan())
                {
                    item(info, index);
                }
            }

            public ParticleLeniaAnalyzerForeachEvent NullableForeach => ForeachEnabled ? Foreach : null;

            public void Loop(ParticleInfoArray result)
            {
                LoopStart(result);

                if (ForeachEnabled)
                {
                    var array = result.Array;
                    for (int i = 0; i < array.Length; i++)
                    {
                        Foreach(array[i], i);
                    }
                }

                LoopEnd(result);
            }
        }

        private readonly List<IParticleLeniaAnalyzer> all = new();
        private IParticleLeniaAnalyzer[] enabled;
        private readonly ParticleLeniaAnalyzerEvents enabledForeachCreated = new(ForeachCreated);
        private readonly ParticleLeniaAnalyzerEvents enabledForeachUpdated = new(ForeachUpdated);
        private readonly ParticleLeniaAnalyzerEvents enabledForeachAfterStep = new(ForeachAfterStep);
        private readonly ParticleLeniaAnalyzerEvents enabledLastAfterStep = new(LastAfterStep);

        public int Count => all.Count;

        public IParticleLeniaAnalyzer this[int index] => all[index];

        public T Search<T>(bool onlyEnabled = true) where T : IParticleLeniaAnalyzer
        {
            if (onlyEnabled)
            {
                foreach (var analyzer in all)
                {
                    if (analyzer.Enabled && analyzer is T ret) return ret;
                }
            }
            else
            {
                foreach (var analyzer in all)
                {
                    if (analyzer is T ret) return ret;
                }
            }

            throw new ArgumentException(nameof(T));
        }

        public List<T> SearchAll<T>(bool onlyEnabled = true) where T : IParticleLeniaAnalyzer
        {
            var list = new List<T>(capacity: 5);

            if (onlyEnabled)
            {
                foreach (var analyzer in all)
                {
                    if (analyzer.Enabled && analyzer is T ret)
                    {
                        list.Add(ret);
                    }
                }
            }
            else
            {
                foreach (var analyzer in all)
                {
                    if (analyzer is T ret)
                    {
                        list.Add(ret);
                    }
                }
            }

            return list;
        }

        public void Add(IParticleLeniaAnalyzer analyzer)
        {
            all.Add(analyzer);
        }

        public void AddRange(IParticleLeniaAnalyzer[] analyzers)
        {
            all.AddRange(analyzers);
        }

        public void Remove(IParticleLeniaAnalyzer analyzer)
        {
            all.Remove(analyzer);
        }

        public void Clear()
        {
            all.Clear();
            CacheClear();
        }

        public void SetupDependencies(IParticleLenia particleLenia)
        {
            CacheMake();

            foreach (var analyzer in all)
            {
                analyzer.SetupDependencies(this, particleLenia);
            }
        }

        public void AnalyzeAndInitWithLastResult(IParticleLenia particleLenia)
        {
            CacheMake();

            foreach (var analyzer in all)
            {
                analyzer.SetupDependencies(this, particleLenia);
                analyzer.Reset();
            }

            var result = particleLenia.GetLastResult();

            enabledForeachCreated.Loop(result);
        }

        public void AnalyzeAndInit(IParticleLenia particleLenia)
        {
            CacheMake();

            foreach (var analyzer in enabled)
            {
                analyzer.SetupDependencies(this, particleLenia);
                analyzer.Reset();
            }

            var result = InitLoop(particleLenia);
            enabledForeachUpdated.Loop(result);
            enabledLastAfterStep.Loop(result);
        }

        public void AnalyzeAndNext(IParticleLenia particleLenia, int step)
        {
            if (step < 0) throw new ArgumentOutOfRangeException(nameof(step));
            if (step == 0)
            {
                var result = particleLenia.GetLastResult();
                enabledForeachUpdated.Loop(result);
                enabledLastAfterStep.Loop(result);
            }
            else
            {
                ParticleInfoArray result;

                for (int i = 0; i < step - 1; i++)
                {
                    result = UpdateLoop(particleLenia);
                    enabledForeachAfterStep.Loop(result);
                }

                result = UpdateLoop(particleLenia);
                enabledLastAfterStep.Loop(result);
            }
        }

        public void RemovePoints(IParticleLenia particleLenia, ReadOnlySpan<int> pointIndexs)
        {
            particleLenia.RemovePoints(pointIndexs);

            foreach (var analyzer in enabled)
            {
                analyzer.RemovePoints(particleLenia, pointIndexs);
            }
        }

        private ParticleInfoArray InitLoop(IParticleLenia particleLenia)
        {
            enabledForeachCreated.LoopStart(null);
            var result = particleLenia.Init(enabledForeachCreated.NullableForeach);
            enabledForeachCreated.LoopEnd(result);

            return result;
        }

        private ParticleInfoArray UpdateLoop(IParticleLenia particleLenia)
        {
            var result = particleLenia.GetLastResult();

            enabledForeachUpdated.LoopStart(result);
            result = particleLenia.Next(enabledForeachUpdated.NullableForeach);
            enabledForeachUpdated.LoopEnd(result);

            return result;
        }

        private void CacheMake()
        {
            CacheClear();

            enabled = all.Where(e => e.Enabled).ToArray();

            foreach (var e in enabled)
            {
                enabledForeachCreated.Add(e);
                enabledForeachUpdated.Add(e);
                enabledForeachAfterStep.Add(e);
                enabledLastAfterStep.Add(e);
            }

            enabledForeachCreated.MakeArray();
            enabledForeachUpdated.MakeArray();
            enabledForeachAfterStep.MakeArray();
            enabledLastAfterStep.MakeArray();
        }

        private void CacheClear()
        {
            enabled = Array.Empty<IParticleLeniaAnalyzer>();
            enabledForeachCreated.Clear();
            enabledForeachUpdated.Clear();
            enabledForeachAfterStep.Clear();
            enabledLastAfterStep.Clear();
        }

        public IEnumerator<IParticleLeniaAnalyzer> GetEnumerator()
        {
            return all.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}