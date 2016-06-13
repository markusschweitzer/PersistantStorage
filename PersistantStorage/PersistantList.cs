using System;
using System.Collections;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;

namespace PersistantStorage
{
	public class PersistantList<T>: MarshalByRefObject, IEnumerable<T> where T : IPersistantListElement
	{
		private readonly PersistantStorage _storage;
		private readonly string _collectionName;

		public PersistantList(PersistantStorage storage, string collectionName)
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

			if(!BsonClassMap.IsClassMapRegistered(typeof(T)))
			{
				BsonClassMap.RegisterClassMap<T>(cm => {
					cm.AutoMap();
				});
			}
		}

		public T this[int id]
		{
			get
			{
				var list = _storage.Retrieve<T>(_collectionName, x => x.Id.Equals(id));
				if(list.Count > 0)
				{
					return list[0];
				}
				return default(T);
			}

			set
			{
				_storage.InsertOrReplace<T>(_collectionName, x => x.Id == id, value);
			}
		}

		public int Count() => (int)_storage.Count<T>(_collectionName, x => true);
		public int Count(System.Linq.Expressions.Expression<Func<T, bool>> filter) => (int)_storage.Count<T>(_collectionName, filter);

		public void Add(T item)
		{
			_storage.Insert<T>(_collectionName, item);
		}

		public void Add(List<T> items)
		{
			_storage.InsertMany<T>(_collectionName, items);
		}

		public void Clear()
		{
			_storage.ClearCollection<T>(_collectionName);
		}

		public bool Contains(T item) => _storage.Retrieve<T>(_collectionName, x => x.Id == item.Id).Count > 0;

		public bool Remove(T item)
		{
			_storage.Delete<T>(_collectionName, x => x.Id == item.Id);
			return true;
		}

		public List<T> ToList() => _storage.Retrieve<T>(_collectionName, x => true);

		public IEnumerator<T> GetEnumerator()
		{
			foreach(T item in ToList())
			{
				yield return item;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach(T item in ToList())
			{
				yield return item;
			}
		}
	}
}
