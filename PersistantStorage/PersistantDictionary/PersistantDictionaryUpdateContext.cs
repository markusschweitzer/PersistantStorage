using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorage
{
    public class PersistantDictionaryUpdateContext<K, T> : MarshalByRefObject, IDisposable
    {
        string _id;
        PersistantDictionary<K, T> _dictionary;
        PersistantDictionaryElement<K, T> _element;
        bool _async;

        public K KeyObject;
        public T DataObject;
        
        public PersistantDictionaryUpdateContext(PersistantDictionary<K, T> dictionary, string id, bool async = false)
        {
            _dictionary = dictionary;
            _id = id;
            _element = dictionary.Get(id);

            KeyObject = _element.KeyObject;
            DataObject = _element.DataObject;

            _async = async;
        }

        public void Dispose()
        {
            _element.KeyObject = KeyObject;
            _element.DataObject = DataObject;

            if (_async)
            {
                _dictionary.UpdateAsync(_id, x => _element);
            }
            else
            {
                _dictionary.Update(_id, x => _element);
            }
        }
    }
}
