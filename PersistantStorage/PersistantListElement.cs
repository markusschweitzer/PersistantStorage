using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorage
{
    public class PersistantListElement<T>
    {
        [BsonId(IdGenerator = typeof(IncrementalIdGenerator))]
        public ObjectId Id { get; set; }
        public T Data { get; set; }

        public PersistantListElement(T data)
        {
            Data = data;
        }
    }
}
