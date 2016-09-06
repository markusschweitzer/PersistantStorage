using MongoDB.Driver;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PersistantStorage
{
    public class PersistantList<T> : MarshalByRefObject, IEnumerable, IPersistantList<T>
    {
        private IMongoCollection<PersistantListElement<T>> _collection;
        private readonly string _collectionName;
        private readonly IMongoDatabase _db;
        private readonly string _dbName;
        private List<PersistantListElement<T>> _localCache;
        private AsyncScheduler _asyncShed;
        private PersistantStorageConnection _connection;

        public PersistantList(PersistantStorageConnection connection, string collection, string database = null)
        {
            _connection = connection;
            if (string.IsNullOrEmpty(database))
            {
                database = connection.DefaultDatabase;
            }
            _db = connection.GetDatabase(database);
            _collection = connection.GetCollection<PersistantListElement<T>>(_db, collection);
            _collectionName = collection;
            _dbName = database;

            var task = _collection.Find(x => true).ToListAsync();
            task.Wait();
            _localCache = task.Result;
            
            _asyncShed = new AsyncScheduler();

            _connection.AddTrackedCollection(_dbName, _collectionName);     
        }

        ~PersistantList()
        {
            _connection.AddTrackedCollection(_dbName, _collectionName);
        }

        public Task<string> AddAsync(T item) => _asyncShed.AddTask(() => Add(item));

        public string Add(T item)
        {
            var newEle = new PersistantListElement<T>(item);
            _collection.InsertOneAsync(newEle).Wait();

            _localCache.Add(newEle);

            return newEle.Id;
        }

        public T Get(string id, bool forceDb = false)
        {
            if (forceDb)
            {
                var currentFind = _collection.Find(x => x.Id.Equals(id)).ToListAsync().Result;
                return currentFind[0].DataObject;
            }
            else
            {
                return _localCache.First(x => x.Id.Equals(id)).DataObject;
            }
        }

        public List<T> Get(Func<T, bool> filter)
        {
            List<T> temp = new List<T>();
            foreach (var ele in _localCache)
            {
                if (filter(ele.DataObject))
                {
                    temp.Add(ele.DataObject);
                }
            }
            return temp;
        }

        public List<string> GetId(Func<T, bool> filter)
        {
            List<string> temp = new List<string>();
            foreach(var ele in _localCache)
            {
                if (filter(ele.DataObject))
                {
                    temp.Add(ele.Id);
                }
            }
            return temp;
        }

        public void Clear()
        {
            _collection.DeleteManyAsync(x => true).Wait();
            _localCache.Clear();
        }

        public int Count() => _localCache.Count;

        public int Count(Func<T, bool> filter)
        {
            int count = 0;
            foreach(var ele in _localCache)
            {
                if (filter(ele.DataObject))
                {
                    ++count;
                }
            }
            return count;
        }

        public void Remove(string id)
        {
            _collection.DeleteOneAsync(x => x.Id.Equals(id)).Wait();

            var ele = _localCache.FirstOrDefault(x => x.Id.Equals(id));
            if (ele != null)
            {
                _localCache.Remove(ele);
            }
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

        public Task UpdateAsync(string id, Func<T, T> update) => _asyncShed.AddTask(() => Update(id, update));

        public void Update(string id, Func<T,T> update)
        {
            var currentFind = _collection.Find(x => x.Id.Equals(id)).ToListAsync().Result;
            if (currentFind.Count == 1) {
                var currentEle = currentFind[0];
                currentEle.DataObject = update(currentEle.DataObject);                
                _collection.ReplaceOneAsync(x => x.Id.Equals(id), currentEle).Wait();

                for (int i = 0; i < _localCache.Count; i++)
                {
                    if (_localCache[i].Id.Equals(id))
                    {
                        _localCache[i] = currentEle;
                    }
                }                
            }
        }

        public PersistantListUpdateContext<T> CreateUpdateContext(string id) => new PersistantListUpdateContext<T>(this, id);

        public PersistantListUpdateContext<T> CreateAsyncUpdateContext(string id) => new PersistantListUpdateContext<T>(this, id, true);

        public List<PersistantListElement<T>> ToList() => _localCache;

        public List<T> ToElementList()
        {
            var temp = new List<T>();
            foreach(var  ele in _localCache)
            {
                temp.Add(ele.DataObject);
            }
            return temp;
        }

        public PersistantListElement<T>[] ToArray() => _localCache.ToArray();

        public T[] ToElementArray()
        {
            return ToElementList().ToArray();
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
            for (int i = _localCache.Count - 1; i >= 0; i--)
            {
                using (var update = CreateUpdateContext(_localCache[i].Id))
                {
                    update.DataObject = action(_localCache[i].DataObject);
                }
            }
        }

        public void ForEachUpdate(Func<PersistantListElement<T>, PersistantListElement<T>> action)
        {
            for (int i = _localCache.Count - 1; i >= 0; i--)
            {
                using (var update = CreateUpdateContext(_localCache[i].Id))
                {
                    var newObject = action(_localCache[i]);
                    update.DataObject = newObject.DataObject;
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

        public string Export(IPersistantSerializer serializer)
        {
            return serializer.Serialize(_localCache);
        }

        public void Import(IPersistantSerializer serializer, string data)
        {
            var temp = serializer.Deserialize<List<PersistantListElement<T>>>(data);
            foreach(var ele in temp)
            {
                Add(ele.DataObject);
            }
        }
    }
}
