using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.ParticleLeniaResarch01.Analysis
{
    public class AutopoieticSystemPotential
    {
        private readonly List<AutopoieticUnitProperty> unitProperties;
        private readonly ConcurrentDictionary<int, double> lastResults;
        private AutopoieticSystemPotentialArg lastArg;

        public int UnitCount { get; private set; }

        public IReadOnlyDictionary<int, double> LastResults => lastResults;

        public AutopoieticSystemPotential()
        {
            unitProperties = new();
            lastResults = new();
        }

        public IReadOnlyDictionary<int, double> CheckPotential(AutopoieticSystemPotentialArg arg)
        {
            lastArg = arg;

            int unitCount = arg.baseUnitAnalyzer.ClusterTrackingDbscan.LastResult.Count;
            UnitCount = unitCount;

            while (unitCount > unitProperties.Count)
            {
                unitProperties.Add(new AutopoieticUnitProperty());
            }

            lastResults.Clear();

            Parallel.For(0, unitCount, UnitPropertyCheck);
            return lastResults;
        }

        private void UnitPropertyCheck(int i)
        {
            var unit = lastArg.baseUnitAnalyzer.ClusterTrackingDbscan.LastResult.Values[i];
            var componentCondition = unitProperties[i];
            lastResults[unit.ClusterID.Value] = componentCondition.CheckProperty(lastArg, unit);
        }
    }
}