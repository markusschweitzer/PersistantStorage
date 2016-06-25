using MongoDB.Driver;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorage
{
    public class PersistantDictionary<K, T> : MarshalByRefObject, IEnumerable, IPersistantDictionary<K, T>
    {
        private IMongoCollection<PersistantDictionaryElement<K, T>> _collection;
        private readonly string _collectionName;
        private readonly IMongoDatabase _db;
        private List<PersistantDictionaryElement<K, T>> _localCache;
        private AsyncScheduler _asyncShed;


        public PersistantDictionary(PersistantStorageConnection connection, string collection, string database = null)
        {
            if (string.IsNullOrEmpty(database))
            {
                database = connection.DefaultDatabase;
            }
            _db = connection.GetDatabase(database);
            _collection = connection.GetCollection<PersistantDictionaryElement<K, T>>(_db, collection);
            _collectionName = collection;

            var task = _collection.Find(x => true).ToListAsync();
            task.Wait();
            _localCache = task.Result;

            _asyncShed = new AsyncScheduler();
        }

        public Task<string> AddAsync(K key, T item) => _asyncShed.AddTask<string>(() => Add(key, item));

        public string Add(K key, T item)
        {
            var newEle = new PersistantDictionaryElement<K, T>(key, item);
            _collection.InsertOneAsync(newEle).Wait();

            _localCache.Add(newEle);

            return newEle.Id;
        }

        public string AddOrReplace(K key, T item)
        {
            string id;
            if(ContainsKey(key, out id))
            {
                id = SetValue(key, item);
            }
            else
            {
                id = Add(key, item);
            }
            return id;
        }

        public T this[K key] => Get(key);

        public T Get(K key)
        {
            var ele = _localCache.FirstOrDefault(x => x.KeyObject.Equals(key));
            if(ele != null)
            {
                return ele.DataObject;
            }
            return default(T);
        }

        public bool TryGetValue(K key, out T value)
        {
            T pVa = Get(key);
            if (pVa == null)
            {
                value = default(T);
                return false;
            }
            else
            {
                value = pVa;
                return true;
            }
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

        public List<string> GetId(Func<K, T, bool> filter)
        {
            List<string> temp = new List<string>();
            foreach (var ele in _localCache)
            {
                if (filter(ele.KeyObject, ele.DataObject))
                {
                    temp.Add(ele.Id);
                }
            }
            return temp;
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

        public int Count() => _localCache.Count;

        public int Count(Func<PersistantDictionaryElement<K,T>, bool> filter)
        {
            int count = 0;
            foreach (var ele in _localCache)
            {
                if (filter(ele))
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
        public Task UpdateAsync(string id, Func<PersistantDictionaryElement<K, T>, PersistantDictionaryElement<K, T>> update) => _asyncShed.AddTask(() => Update(id, update));

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

        public PersistantDictionaryUpdateContext<K, T> CreateAsyncUpdateContext(string id) => new PersistantDictionaryUpdateContext<K, T>(this, id, true);
        public PersistantDictionaryUpdateContext<K, T> CreateUpdateContext(string id) => new PersistantDictionaryUpdateContext<K, T>(this, id);

        public List<PersistantDictionaryElement<K, T>> ToList() => _localCache;

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


        public string Export(IPersistantSerializer serializer)
        {
            return serializer.Serialize(_localCache);
        }

        public void Import(IPersistantSerializer serializer, string data)
        {
            var temp = serializer.Deserialize<List<PersistantDictionaryElement<K, T>>>(data);
            foreach (var ele in temp)
            {
                Add(ele.KeyObject, ele.DataObject);
            }
        }
    }
}
