﻿using MongoDB.Driver;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorage
{
    public class PersistantDictionary<K, T> : IEnumerable
    {
        private IMongoCollection<PersistantDictionaryElement<K, T>> _collection;
        private readonly string _collectionName;
        private readonly IMongoDatabase _db;
        private List<PersistantDictionaryElement<K, T>> _localCache;


        public PersistantDictionary(PersistantStorageConnection connection, string database, string collection)
        {
            _db = connection.GetDatabase(database);
            _collection = connection.GetCollection<PersistantDictionaryElement<K, T>>(_db, collection);
            _collectionName = collection;

            var task = _collection.Find(x => true).ToListAsync();
            task.Wait();
            _localCache = task.Result;
        }

        public PersistantDictionary(IMongoDatabase database, string collection)
        {
            _db = database;
            _collection = _db.GetCollection<PersistantDictionaryElement<K, T>>(collection);
            _collectionName = collection;

            var task = _collection.Find(x => true).ToListAsync();
            task.Wait();
            _localCache = task.Result;
        }

        public PersistantDictionary(IMongoCollection<PersistantDictionaryElement<K, T>> collection)
        {
            _db = collection.Database;
            _collection = collection;
            _collectionName = collection.CollectionNamespace.CollectionName;

            var task = _collection.Find(x => true).ToListAsync();
            task.Wait();
            _localCache = task.Result;
        }

        public string Add(K key, T item)
        {
            var newEle = new PersistantDictionaryElement<K, T>(key, item);
            _collection.InsertOneAsync(newEle).Wait();

            _localCache.Add(newEle);

            return newEle.Id;
        }

        public T Get(K key)
        {
            var ele = _localCache.FirstOrDefault(x => x.KeyObject.Equals(key));
            if(ele != null)
            {
                return ele.DataObject;
            }
            return default(T);
        }

        public bool ContainsKey(K key, out string id)
        {
            var ele = _localCache.FirstOrDefault(x => x.KeyObject.Equals(key));
            if(ele != null)
            {
                id = ele.Id;
                return true;
            }
            else
            {
                id = null;
                return false;
            }
        }

        public bool ContainsValue(T value, out string id)
        {
            var ele = _localCache.FirstOrDefault(x => x.DataObject.Equals(value));
            if (ele != null)
            {
                id = ele.Id;
                return true;
            }
            else
            {
                id = null;
                return false;
            }

        }

        public string SetValue(K key, T value)
        {
            string id = null;
            if (!ContainsKey(key, out id))
            {
                return Add(key, value);
            }
            else
            {
                using(var update = CreateUpdateContext(id))
                {
                    update.DataObject = value;
                }
                return id;
            }
        }

        public PersistantDictionaryElement<K, T> Get(string id, bool forceDb = false)
        {
            if (forceDb)
            {
                var currentFind = _collection.Find(x => x.Id.Equals(id)).ToListAsync().Result;
                return currentFind[0];
            }
            else
            {
                return _localCache.First(x => x.Id.Equals(id));
            }
        }

        public string GetId(Func<K, T, bool> filter)
        {
            foreach (var ele in _localCache)
            {
                if (filter(ele.KeyObject, ele.DataObject))
                {
                    return ele.Id;
                }
            }
            return null;
        }

        public T GetOrDefaultValue(K key, T defaultValue, bool save = false)
        {
            T val = Get(key);
            if(val != null)
            {
                return val;
            }
            else
            {
                if (save)
                {
                    Add(key, defaultValue);
                }
                return defaultValue;
            }
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
            foreach (var id in ids)
            {
                _localCache.Remove(_localCache.First(x => x.Id.Equals(id)));
            }
        }

        public void Remove(Func<K, T, bool> filter)
        {
            var temp = new List<string>();
            foreach (var ele in _localCache)
            {
                if (filter(ele.KeyObject, ele.DataObject))
                {
                    temp.Add(ele.Id);
                }
            }

            Remove(temp);
        }

        public void Update(string id, Func<PersistantDictionaryElement<K, T>, PersistantDictionaryElement<K, T>> update)
        {
            var currentFind = _collection.Find(x => x.Id.Equals(id)).ToListAsync().Result;
            if (currentFind.Count == 1)
            {
                var currentEle = currentFind[0];
                currentEle = update(currentEle);
                currentEle.Id = id;
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

        public PersistantDictionaryUpdateContext<K, T> CreateUpdateContext(string id) => new PersistantDictionaryUpdateContext<K, T>(this, id);

        public IReadOnlyList<PersistantDictionaryElement<K, T>> ToList() => _localCache;

        public Dictionary<K, T> ToDictionary()
        {
            var temp = new Dictionary<K, T>();
            foreach(var ele in _localCache)
            {
                temp.Add(ele.KeyObject, ele.DataObject);
            }
            return temp;
        }
        
        public PersistantDictionaryElement<K, T>[] ToArray() => _localCache.ToArray();

        public void ForEach(Action<PersistantDictionaryElement<K, T>> action)
        {
            _localCache.ForEach(action);
        }

        public void ForEachUpdate(Func<PersistantDictionaryElement<K, T>, PersistantDictionaryElement<K, T>> action)
        {
            for (int i = _localCache.Count - 1; i >= 0; i--)
            {
                using (var update = CreateUpdateContext(_localCache[i].Id))
                {
                    var intermediate = action(_localCache[i]);
                    update.KeyObject = intermediate.KeyObject;
                    update.DataObject = intermediate.DataObject;
                }
            }
        }

        public void ResetCollection(bool keepEntries)
        {
            _db.DropCollectionAsync(_collectionName).Wait();
            _collection = _db.GetCollection<PersistantDictionaryElement<K, T>>(_collectionName);

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

        public IEnumerator<PersistantDictionaryElement<K, T>> GetEnumerator()
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
