using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorage
{
    public class PersistantListUpdateContext<T> : IDisposable
    {
        string _id;
        PersistantList<T> _list;

        public T DataObject;
        public PersistantListUpdateContext(PersistantList<T> list, string id)
        {
            _list = list;
            _id = id;
            DataObject = list.Get(id);
        }

        public void Dispose()
        {
            _list.Update(_id, x => DataObject);
        }
    }
}
