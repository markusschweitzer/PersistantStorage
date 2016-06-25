using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorage
{
    public class PersistantListUpdateContext<T> : MarshalByRefObject, IDisposable
    {
        string _id;
        PersistantList<T> _list;
        bool _async;

        public T DataObject;
        public PersistantListUpdateContext(PersistantList<T> list, string id, bool async = false)
        {
            _list = list;
            _id = id;
            DataObject = list.Get(id);
            _async = async;
        }

        public void Dispose()
        {
            if (_async)
            {
                _list.UpdateAsync(_id, x => DataObject);
            }
            else
            {
                _list.Update(_id, x => DataObject);
            }
        }
    }
}
