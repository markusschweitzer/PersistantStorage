using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorage
{
    public class PersistantDictionaryElement<K, T> : PersistantListElement<T>
    {
        public K KeyObject;

        public PersistantDictionaryElement(K key, T item) : base(item)
        {
            KeyObject = key;
        }

    }
}
