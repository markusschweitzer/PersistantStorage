using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using System;
using System.Collections;
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

    public class PersistantList<T> : IEnumerable
    {
        private readonly MongoClient _client;
        private IMongoCollection<PersistantListElement<T>> _collection;
        private readonly string _collectionName;
        private readonly IMongoDatabase _db;
        private List<PersistantListElement<T>> _localCache;

        public PersistantList(string  connectionString, string database, string collection)
        {
            _client = new MongoClient(connectionString);
            _db = _client.GetDatabase(database);
            _collection = _db.GetCollection<PersistantListElement<T>>(collection);
            _collectionName = collection;

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

        public T Get(string id) => _localCache.First(x => x.Id.Equals(id)).DataObject;

        public string GetId(Func<T, bool> filter)
        {
            foreach(var ele in _localCache)
            {
                if (filter(ele.DataObject))
                {
                    return ele.Id;
                }
            }
            return null;
        }

        public void Clear()
        {
            _collection.DeleteManyAsync(x => true).Wait();
            _localCache.Clear();
        }

        public long Count() => _localCache.Count;

        public void Remove(string id)
        {
            _collection.DeleteOneAsync(x => x.Id.Equals(id)).Wait();
            _localCache.Remove(_localCache.First(x => x.Id.Equals(id)));
        }

        public void Remove(List<string> ids)
        {
            _collection.DeleteManyAsync(x => ids.Contains(x.Id)).Wait();
            foreach(var id in ids)
            {
                _localCache.Remove(_localCache.First(x => x.Id.Equals(id)));
            }
        }

        public void Remove(Func<T, bool> filter)
        {
            var temp = new List<string>();
            foreach(var ele in _localCache)
            {
                if (filter(ele.DataObject))
                {
                    temp.Add(ele.Id);
                }
            }

            Remove(temp);
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

        public PersistantListUpdateContext<T> CreateUpdateContext(string id) => new PersistantListUpdateContext<T>(this, id);

        public IReadOnlyList<PersistantListElement<T>> ToList() => _localCache;

        public IReadOnlyList<T> ToElementList()
        {
            var temp = new List<T>();
            foreach(var  ele in _localCache)
            {
                temp.Add(ele.DataObject);
            }
            return temp;
        }

        public void ForEach(Action<PersistantListElement<T>> action)
        {
            _localCache.ForEach(action);
        }

        public void ForEachElement(Action<T> action)
        {
            var temp = ToElementList().ToList();
            temp.ForEach(action);
        }

        public void ForEachElementUpdate(Func<T, T> action)
        {
            foreach (var ele in _localCache)
            {
                using (var update = CreateUpdateContext(ele.Id))
                {
                    update.DataObject = action(ele.DataObject);
                }
            }
        }
        
        public void ResetCollection(bool keepEntries)
        {
            _db.DropCollectionAsync(_collectionName).Wait();
            _collection = _db.GetCollection<PersistantListElement<T>>(_collectionName);

            if (keepEntries)
            {
                _collection.InsertManyAsync(_localCache);
            }
            else
            {
                var task = _collection.Find(x => true).ToListAsync();
                task.Wait();
                _localCache = task.Result;
            }
        }

        public IEnumerator<PersistantListElement<T>> GetEnumerator()
        {
            foreach (var ele in _localCache)
            {
                yield return ele;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var ele in _localCache)
            {
                yield return ele;
            }
        }
    }
}
