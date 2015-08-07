using System.Collections.Generic;
using InfluxDB.Net.Collector.Interface;

namespace InfluxDB.Net.Collector.Entities
{
    public class PerformanceCounterGroup : IPerformanceCounterGroup
    {
        private readonly string _name;
        private readonly int _secondsInterval;
        private readonly IReadOnlyCollection<IPerformanceCounterInfo> _performanceCounterInfos;

        public PerformanceCounterGroup(string name, int secondsInterval, IReadOnlyCollection<IPerformanceCounterInfo> performanceCounterInfos)
        {
            _name = name;
            _secondsInterval = secondsInterval;
            _performanceCounterInfos = performanceCounterInfos;
        }

        public string Name { get { return _name; } }
        public int SecondsInterval { get { return _secondsInterval; } }
        public IEnumerable<IPerformanceCounterInfo> PerformanceCounterInfos { get { return _performanceCounterInfos; } }
    }
}