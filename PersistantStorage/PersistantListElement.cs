using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorage
{
    public class PersistantListElement<T>
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        public string Id;

        public T DataObject;

        public PersistantListElement(T item)
        {
            DataObject = item;
        }
    }
}
