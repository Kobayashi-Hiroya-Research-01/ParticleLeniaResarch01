using Assets.ParticleLeniaResarch01.Analyzers;
using Assets.ParticleLeniaResarch01.Scripts;
using Assets.ParticleLeniaResarch01.Scripts.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.tvOS;

namespace Assets.ParticleLeniaResarch01.Analysis
{
    using Cluster = ClusterTrackingCluster<ClusterTrackingPoint>;
    using ClusteringAnalyzer = ClusterTrackingDbscanAnalyzer;

    public class AutopoieticComponentCondition
    {
        private class Simulation
        {
            public readonly ParticleLeniaSimulation simulation;
            public readonly ParticleLeniaAnalyzer analyzers;
            public readonly MinMaxAnalyzer minMaxAnalyzer;
            public readonly ClusteringAnalyzer componentAnalyzer;
            public readonly ClusteringAnalyzer unitAnalyzer;

            public Simulation()
            {
                simulation = new ParticleLeniaSimulation();
                minMaxAnalyzer = new MinMaxAnalyzer();
                componentAnalyzer = new ClusteringAnalyzer();
                unitAnalyzer = new ClusteringAnalyzer();
                analyzers = new ParticleLeniaAnalyzer()
                {
                    minMaxAnalyzer, componentAnalyzer, unitAnalyzer
                };

                analyzers.SetupDependencies(simulation);
            }

            public void Init(ParticleLeniaSimulation baseSimulation, ClusteringAnalyzer baseComponentAnalyzer, ClusteringAnalyzer baseUnitAnalyzer)
            {
                baseSimulation.CopyTo(simulation);
                CopyAnalyzers(baseComponentAnalyzer, baseUnitAnalyzer);
            }

            public void Init(ParticleLeniaSimulation baseSimulation, ClusteringAnalyzer baseComponentAnalyzer, ClusteringAnalyzer baseUnitAnalyzer, ReadOnlySpan<int> removes)
            {
                baseSimulation.CopyTo(simulation);
                CopyAnalyzers(baseComponentAnalyzer, baseUnitAnalyzer);
                analyzers.RemovePoints(simulation, removes);
            }

            private void CopyAnalyzers(ClusteringAnalyzer baseComponentAnalyzer, ClusteringAnalyzer baseUnitAnalyzer)
            {
                baseComponentAnalyzer.AsOption.CopyTo(componentAnalyzer);
                baseUnitAnalyzer.AsOption.CopyTo(unitAnalyzer);

                minMaxAnalyzer.Reset();
                componentAnalyzer.Reset();
                unitAnalyzer.Reset();

                var result = simulation.GetLastResult();
                var resultArray = result.Array;

                for (int i = 0; i < resultArray.Length; i++)
                {
                    var item = resultArray[i];
                    minMaxAnalyzer.Foreach(item, i);
                    componentAnalyzer.ForeachCreated(item, i);
                    unitAnalyzer.ForeachCreated(item, i);
                }

                componentAnalyzer.AllUpdated(result);
                unitAnalyzer.AllUpdated(result);

                baseComponentAnalyzer.CopyIDTo(componentAnalyzer);
                baseUnitAnalyzer.CopyIDTo(unitAnalyzer);
            }

            public void AnalyzeAndNext(int step)
            {
                analyzers.AnalyzeAndNext(simulation, step);
            }

            public Cluster GetComponent(int id)
            {
                componentAnalyzer.ClusterTrackingDbscan.LastResult.UpdateIDs();
                return componentAnalyzer.ClusterTrackingDbscan.LastResult[id];
            }

            public Cluster GetUnit(int id)
            {
                unitAnalyzer.ClusterTrackingDbscan.LastResult.UpdateIDs();
                return unitAnalyzer.ClusterTrackingDbscan.LastResult[id];
            }
        }

        private readonly Simulation simulation;
        public bool LastResult { get; private set; }

        public AutopoieticComponentCondition()
        {
            simulation = new Simulation();
        }

        public bool CheckCondition(AutopoieticSystemPotentialArg arg, Cluster unit, Cluster component)
        {
            if (component.Count > unit.Count) return false;

            var step = arg.step;
            var baseSimulation = arg.baseSimulation;
            var baseUnitAnalyzer = arg.baseUnitAnalyzer;
            var baseComponentAnalyzer = arg.baseComponentAnalyzer;

            Span<int> unitDashParticleIndexs = stackalloc int[unit.Count - component.Count];
            Span<int> componentParticleIndexs = stackalloc int[component.Count];

            //a.i. : C ⊆ U
            var condA1 = GetUnitDash(component, unit, unitDashParticleIndexs);
            if (!condA1)
            {
                LastResult = false;
                return false;
            }

            var componentID = component.ClusterID.Value;
            var unitID = unit.ClusterID.Value;

            //var pair = $"[{unitID}, {componentID}]";
            //var log1 = ClusteringLog(baseComponentAnalyzer);

            simulation.Init(baseSimulation, baseComponentAnalyzer, baseUnitAnalyzer);
            simulation.AnalyzeAndNext(step);

            /*
            var log2 = ClusteringLog(simulation.componentAnalyzer);

            var result = simulation.simulation.GetLastResult();

            simulation.AnalyzeAndNext(0);
            //simulation.componentAnalyzer.AllUpdatedTest(result);
            //simulation.unitAnalyzer.AllUpdatedTest(result);

            var log3 = ClusteringLog(simulation.componentAnalyzer);

            simulation.componentAnalyzer.AllUpdated(result);
            simulation.unitAnalyzer.AllUpdated(result);

            var log4 = ClusteringLog(simulation.componentAnalyzer);
            */

            var componentClusterT = simulation.GetComponent(componentID);
            var unitClusterT = simulation.GetUnit(unitID);

            //b.i. : I(C(τ, N)) = I(C)      「U'がある時、τ時間後に同一の I(C) を観察できる」
            if (componentClusterT == null)
            {
                // Debug.Log("b.i");
                LastResult = false;
                return false;
            }

            //c.i. : I(U(τ, N)) = I(U)      「C がある時、τ時間後に同一の I(U) を観察できる」
            if (unitClusterT == null)
            {
                //Debug.Log("c.i");
                LastResult = false;
                return false;
            }

            //a.ii. : C(τ, N) ⊆ U(τ, N)
            var condA2 = IsSubset(componentClusterT, unitClusterT);
            if (!condA2)
            {
                //Debug.Log("a.ii");
                LastResult = false;
                return false;
            }

            //LastResult = true;
            //return true;
            Sort(unitDashParticleIndexs);

            simulation.Init(baseSimulation, baseComponentAnalyzer, baseUnitAnalyzer, unitDashParticleIndexs);
            simulation.AnalyzeAndNext(step);

            //b.ii. : I(C(τ, N - U')) ≠ I(C)    「U'が無い時、τ 時間後に同一の I(C) を観察できない」
            if (simulation.GetComponent(componentID) != null)
            {
                //Debug.Log("b.ii");
                LastResult = false;
                return false;
            }

            for (int i = 0; i < componentParticleIndexs.Length; i++)
            {
                componentParticleIndexs[i] = component[i].Index;
            }

            Sort(componentParticleIndexs);

            simulation.Init(baseSimulation, baseComponentAnalyzer, baseUnitAnalyzer, componentParticleIndexs);
            simulation.AnalyzeAndNext(step);

            //c.ii. : I(U(τ, N - C )) ≠ I(U)    「C が無い時、τ 時間後に同一の I(U) を観察できない」
            if (simulation.GetUnit(unitID) != null)
            {
                //Debug.Log("c.ii");
                LastResult = false;
                return false;
            }

            Debug.Log("Autopoietic Component");
            LastResult = true;
            return true;
        }

        public static void Sort(Span<int> span)
        {
            if (span.Length < 2) return;

            int pivot = span[span.Length / 2];
            int i = 0, j = span.Length - 1;

            while (i <= j)
            {
                while (span[i] < pivot) i++;
                while (span[j] > pivot) j--;
                if (i <= j)
                {
                    (span[i], span[j]) = (span[j], span[i]);
                    i++; j--;
                }
            }

            Sort(span[..(j + 1)]);
            Sort(span[i..]);
        }

        private readonly HashSet<int> _Intersect_hash = new();

        private static bool IsSubset(Cluster component, Cluster unit)
        {
            if (component.Count > unit.Count) return false;

            foreach (var c in component)
            {
                var found = false;
                var ci = c.Index;

                foreach (var u in unit)
                {
                    if (u.Index == ci)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool GetUnitDash(Cluster component, Cluster unit, Span<int> unitDash)
        {
            if (component.Count > unit.Count) return false;

            Span<bool> removed = stackalloc bool[unit.Count];

            foreach (var c in component)
            {
                var found = false;
                var ci = c.Index;

                for (int ui = 0; ui < unit.Count; ui++)
                {
                    var u = unit[ui];
                    if (u.Index == ci)
                    {
                        removed[ui] = true;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            int unitDashIndex = 0;
            for (int i = 0; i < removed.Length; i++)
            {
                if (!removed[i])
                    unitDash[unitDashIndex++] = unit[i].Index;
            }

            return true;
        }

        private static string ClusteringLog(ClusteringAnalyzer analyzer)
        {
            var result = analyzer.ClusterTrackingDbscan.LastResult;
            var lastID = analyzer.AsState.ClusterTrackingState.LastID;
            var startIndex = analyzer.AsOption.ClusterTrackingOption.StartIndex;

            var clusters = new List<Cluster>(10);
            var objects = new List<ClusterTrackingPoint>(200);

            foreach (var cluster in result.Values)
            {
                if (objects.Count < 20) objects.AddRange(cluster.Objects);
                clusters.Add(cluster);
            }

            while (objects.Count > 20)
            {
                objects.RemoveAt(objects.Count - 1);
            }

            var text = $"{{ startIndex={startIndex}, lastID={lastID} clusters=[{string.Join(", ", clusters.Select(e => $"[{e.ClusterID}:{e.Objects.Count},{e.LifeTime}]"))}] objects=[{string.Join(", ", objects)}] }}";

            return text;
        }
    }
}