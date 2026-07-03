using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zappy
{
    public class Inventory
    {
        private Dictionary<Resource.ResourceType, int> data = new();

        public event Action Changed;

        public Inventory()
        {
            foreach (Resource.ResourceType t in Enum.GetValues(typeof(Resource.ResourceType)))
                data[t] = 0;
        }

        public void Set(Resource.ResourceType type, int amount)
        {
            data[type] = amount;
            Changed?.Invoke();
        }

        public void Add(Resource.ResourceType type, int amount)
        {
            data[type] += amount;
            Changed?.Invoke();
        }

        public bool Remove(Resource.ResourceType type, int amount)
        {
            if (data[type] < amount)
                return false;

            data[type] -= amount;
            Changed?.Invoke();
            return true;
        }

        public int Get(Resource.ResourceType type) => data[type];

        public IReadOnlyDictionary<Resource.ResourceType, int> All => data;

        /// <summary>
        /// Vista de solo lectura en orden ESTABLE (el orden del enum ResourceType),
        /// para iteración determinista. El orden de un Dictionary no está garantizado.
        /// </summary>
        public IEnumerable<KeyValuePair<Resource.ResourceType, int>> AllOrdered =>
            Enum.GetValues(typeof(Resource.ResourceType))
                .Cast<Resource.ResourceType>()
                .Select(t => new KeyValuePair<Resource.ResourceType, int>(t, data[t]));
    }
}
