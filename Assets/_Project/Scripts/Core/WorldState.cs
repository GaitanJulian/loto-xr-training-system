using System.Collections.Generic;

namespace Project.Core
{
    public sealed class WorldState
    {
        private readonly Dictionary<string, bool> _bools = new();

        public void SetBool(string key, bool value)
        {
            _bools[key] = value;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            if (_bools.TryGetValue(key, out var value))
                return value;

            return defaultValue;
        }

        public bool HasBool(string key) => _bools.ContainsKey(key);
    }
}
