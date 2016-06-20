using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorage
{
    public class PersistantDictionaryUpdateContext<K, T> : IDisposable
    {
        string _id;
        PersistantDictionary<K, T> _dictionary;
        PersistantDictionaryElement<K, T> _element;

        public K KeyObject;
        public T DataObject;
        
        public PersistantDictionaryUpdateContext(PersistantDictionary<K, T> dictionary, string id)
        {
            _dictionary = dictionary;
            _id = id;
            _element = dictionary.Get(id);

            KeyObject = _element.KeyObject;
            DataObject = _element.DataObject;
        }

        public void Dispose()
        {
            _element.KeyObject = KeyObject;
            _element.DataObject = DataObject;
            _dictionary.Update(_id, x => _element);
        }
    }
}
