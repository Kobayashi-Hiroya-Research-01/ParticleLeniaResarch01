using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assets.ParticleLeniaResarch01.Analysis
{
    using Cluster = ClusterTrackingCluster<ClusterTrackingPoint>;

    public class AutopoieticUnitProperty
    {
        private readonly List<AutopoieticComponentCondition> componentConditions;
        private AutopoieticSystemPotentialArg lastArg;
        private Cluster lastArgUnit;

        public double LastResult { get; private set; }

        public AutopoieticUnitProperty()
        {
            componentConditions = new List<AutopoieticComponentCondition>();
        }

        public double CheckProperty(AutopoieticSystemPotentialArg arg, Cluster unit)
        {
            lastArg = arg;
            lastArgUnit = unit;

            int componentCount = arg.baseComponentAnalyzer.ClusterTrackingDbscan.LastResult.Count;
            while (componentCount > componentConditions.Count)
            {
                componentConditions.Add(new AutopoieticComponentCondition());
            }

            Parallel.For(0, componentCount, ComponentConditionCheck);

            var componentConditionResults = componentConditions.Take(componentCount).Count(e => e.LastResult);
            var result = (double)componentConditionResults / componentCount;
            LastResult = result;
            return result;
        }

        private void ComponentConditionCheck(int i)
        {
            var component = lastArg.baseComponentAnalyzer.ClusterTrackingDbscan.LastResult.Values[i];
            var componentCondition = componentConditions[i];
            componentCondition.CheckCondition(lastArg, lastArgUnit, component);
        }
    }
}