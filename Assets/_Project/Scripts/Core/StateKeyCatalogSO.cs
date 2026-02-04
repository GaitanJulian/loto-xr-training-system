using System.Collections.Generic;
using UnityEngine;

namespace Project.Core
{
    [CreateAssetMenu(menuName = "XR Training/State Key Catalog", fileName = "StateKeyCatalog")]
    public sealed class StateKeyCatalogSO : ScriptableObject
    {
        [SerializeField] private List<string> keys = new();

        public IReadOnlyList<string> Keys => keys;

        public bool Contains(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            return keys.Contains(key);
        }
    }
}
