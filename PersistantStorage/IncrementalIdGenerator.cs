using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorage
{
    public class IncrementalIdGenerator : IIdGenerator
    {
        long _lastId = 0;
        public object GenerateId(object container, object document)
        {
            _lastId++;
            return _lastId;
        }

        public bool IsEmpty(object id)
        {
            if(id != null)
            {
                var temp = (long)id;
                if(temp >= 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
