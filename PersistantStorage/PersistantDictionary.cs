using System;
using System.Collections;
using System.Collections.Generic;

namespace PersistantStorage
{
	public class PersistantDictionary<TKey, TValue>: MarshalByRefObject, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
	{
		private readonly PersistantStorage _storage;
		private readonly string _collectionName;

		private readonly PersistantList<NameValue<TKey, TValue>> _internalList;

		public PersistantDictionary(PersistantStorage storage, string collectionName)
		{
			if(!storage.Connected)
			{
				throw new ArgumentException("PersisnatStorage needs to be connected first.");
			}

			if(string.IsNullOrEmpty(collectionName))
			{
				throw new ArgumentException("Please yupply a valid collectionName");
			}

			_storage = storage;
			_collectionName = collectionName;
			_internalList = new PersistantList<NameValue<TKey, TValue>>(_storage, collectionName);
		}
		public int Count() => _internalList.Count();
		public int Count(System.Linq.Expressions.Expression<Func<NameValue<TKey, TValue>, bool>> filter) => _internalList.Count(filter);

		public TValue this[TKey key]
		{
			get
			{
				var list = _storage.Retrieve<NameValue<TKey, TValue>>(_collectionName, x => x.Name.Equals(key));
				if(list.Count > 0)
				{
					return list[0].Value;
				}
				return default(TValue);
			}

			set
			{
				_storage.InsertOrReplace(_collectionName, x => x.Name.Equals(key), new NameValue<TKey, TValue>(key, value));
			}
		}

		public void AddOrReplace(TKey key, TValue value, bool forceDelete = false)
		{
			_storage.InsertOrReplace(_collectionName, x => x.Name.Equals(key), new NameValue<TKey, TValue>(key, value), forceDelete);
		}

		public void AddOrReplace(Dictionary<TKey, TValue> values)
		{
			foreach(var v in values)
			{
				_storage.InsertOrReplace(_collectionName, x => x.Name.Equals(v.Key), new NameValue<TKey, TValue>(v.Key, v.Value));
			}
		}

		public void Add(TKey key, TValue value)
		{
			if(!Contains(key))
			{
				_storage.Insert(_collectionName, new NameValue<TKey, TValue>(key, value));
			}
			else
			{
				throw new ArgumentException(string.Format("A key with the name '{0}' is already in the collection", key.ToString()));
			}
		}

		public void Add(Dictionary<TKey, TValue> values)
		{
			var temp = new List<NameValue<TKey, TValue>>();
			foreach(var v in values)
			{
				temp.Add(new NameValue<TKey, TValue>(v.Key, v.Value));
			}
			_storage.InsertMany(_collectionName, temp);
		}

		public void Remove(TKey key)
		{
			_storage.Delete<NameValue<TKey, TValue>>(_collectionName, x => x.Name.Equals(key));
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			TValue pVa = this[key];
			if(pVa == null)
			{
				value = default(TValue);
				return false;
			}
			else
			{
				value = pVa;
				return true;
			}
		}

		public TValue GetOrDefaultValue(TKey key, TValue defaultValue, bool save = false)
		{
			TValue pVa = this[key];
			if(pVa == null)
			{
				if(save)
				{
					Add(key, defaultValue);
				}
				return defaultValue;
			}
			else
			{
				return pVa;
			}
		}

		public void Clear()
		{
			_storage.ClearCollection<NameValue<TKey, TValue>>(_collectionName);
		}

		public bool Contains(TKey key)
		{
			try
			{
				return !this[key].Equals(default(TValue));
			}
			catch
			{
				return false;
			}
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}

		public bool Contains(TKey key, TValue value)
		{
			var list = _storage.Retrieve<NameValue<TKey, TValue>>(_collectionName, x => x.Name.Equals(key));
			if(list.Count > 0)
			{
				return list[0].Value.Equals(value);
			}
			return false;
		}

		public void Remove(TKey key, TValue value)
		{
			_storage.Delete<NameValue<TKey, TValue>>(_collectionName, x => x.Name.Equals(key) && x.Value.Equals(value));
		}

		public Dictionary<TKey, TValue> ToDictionary()
		{
			var list = _storage.Retrieve<NameValue<TKey, TValue>>(_collectionName, x => true);

			var temp = new Dictionary<TKey, TValue>();
			foreach(var k in list)
			{
				temp.Add(k.Name, k.Value);
			}
			return temp;
		}
	}
}
