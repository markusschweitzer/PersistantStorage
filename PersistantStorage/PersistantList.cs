using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

    public class PersistantList<T>
    {
        private readonly MongoClient _client;
        private readonly IMongoCollection<PersistantListElement<T>> _collection;
        private readonly IMongoDatabase _db;
        private readonly List<PersistantListElement<T>> _localCache;

        public PersistantList(string  connectionString, string database, string collection)
        {
            _client = new MongoClient(connectionString);
            _db = _client.GetDatabase(database);
            _collection = _db.GetCollection<PersistantListElement<T>>(collection);

            var task = _collection.Find(x => true).ToListAsync();
            task.Wait();
            _localCache = task.Result;
        }

        public string Add(T item)
        {
            var newEle = new PersistantListElement<T>(item);
            _collection.InsertOneAsync(newEle).Wait();

            _localCache.Add(newEle);

            return newEle.Id;
        }

        public T Get(string id)
        {
            return _localCache.First(x => x.Id.Equals(id)).DataObject;
        }

        public void Clear()
        {
            _collection.DeleteManyAsync(x => true).Wait();
            _localCache.Clear();
        }

        public long Count()
        {
            return _localCache.Count;
        }

        public void Remove(string id)
        {
            _collection.DeleteOneAsync(x => x.Id.Equals(id)).Wait();
            _localCache.Remove(_localCache.First(x => x.Id.Equals(id)));
        }

        public void Update(string id, Func<T,T> update)
        {
            var currentFind = _collection.Find(x => x.Id.Equals(id)).ToListAsync().Result;
            if (currentFind.Count == 1) {
                var currentEle = currentFind[0];
                currentEle.DataObject = update(currentEle.DataObject);                
                _collection.ReplaceOneAsync(x => x.Id.Equals(id), currentEle).Wait();

                var localEle = _localCache.Where(x => x.Id.Equals(id)).ToList();
                localEle[0] = currentEle;
            }
        }

        public PersistantListUpdateContext<T> CreateUpdateContext(string id)
        {
            return new PersistantListUpdateContext<T>(this, id);
        }

        public IReadOnlyList<PersistantListElement<T>> ToList()
        {
            return _localCache;
        }

        public void ResetDbCollection()
        {
            
        }
    }
}
