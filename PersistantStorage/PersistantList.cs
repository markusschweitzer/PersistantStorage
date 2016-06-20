using MongoDB.Driver;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PersistantStorage
{
    public class PersistantList<T> : IEnumerable
    {
        private IMongoCollection<PersistantListElement<T>> _collection;
        private readonly string _collectionName;
        private readonly IMongoDatabase _db;
        private List<PersistantListElement<T>> _localCache;


        public PersistantList(PersistantStorageConnection connection, string database, string collection)
        {
            _db = connection.GetDatabase(database);
            _collection = connection.GetCollection<PersistantListElement<T>>(_db, collection);
            _collectionName = collection;

            var task = _collection.Find(x => true).ToListAsync();
            task.Wait();
            _localCache = task.Result;
        }

        public PersistantList(IMongoDatabase database, string collection)
        {
            _db = database;
            _collection = _db.GetCollection<PersistantListElement<T>>(collection);
            _collectionName = collection;

            var task = _collection.Find(x => true).ToListAsync();
            task.Wait();
            _localCache = task.Result;
        }

        public PersistantList(IMongoCollection<PersistantListElement<T>> collection)
        {
            _db = collection.Database;
            _collection = collection;
            _collectionName = collection.CollectionNamespace.CollectionName;

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
            for (int i = _localCache.Count-1; i >= 0; i--)
            {
                using (var update = CreateUpdateContext(_localCache[i].Id))
                {
                    update.DataObject = action(_localCache[i].DataObject);
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
