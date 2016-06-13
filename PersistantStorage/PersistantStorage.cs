using System;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Conventions;

namespace PersistantStorage
{
	internal class DictionaryRepresentationConvention: ConventionBase, IMemberMapConvention
	{
		private readonly DictionaryRepresentation _dictionaryRepresentation;
		public DictionaryRepresentationConvention(DictionaryRepresentation dictionaryRepresentation)
		{
			_dictionaryRepresentation = dictionaryRepresentation;
		}
		public void Apply(BsonMemberMap memberMap)
		{
			memberMap.SetSerializer(ConfigureSerializer(memberMap.GetSerializer()));
		}
		private IBsonSerializer ConfigureSerializer(IBsonSerializer serializer)
		{
			var dictionaryRepresentationConfigurable = serializer as IDictionaryRepresentationConfigurable;
			if(dictionaryRepresentationConfigurable != null)
			{
				serializer = dictionaryRepresentationConfigurable.WithDictionaryRepresentation(_dictionaryRepresentation);
			}

			var childSerializerConfigurable = serializer as IChildSerializerConfigurable;
			return childSerializerConfigurable == null
				? serializer
				: childSerializerConfigurable.WithChildSerializer(ConfigureSerializer(childSerializerConfigurable.ChildSerializer));
		}
	}
	public class PersistantStorage: MarshalByRefObject
	{
		private MongoClient _client = null;
		private IMongoDatabase _db = null;

		public bool Connected => _client != null;

		public void Connect(string connectionString, string database)
		{
			ConventionRegistry.Register(
	"DictionaryRepresentationConvention",
	new ConventionPack { new DictionaryRepresentationConvention(DictionaryRepresentation.ArrayOfArrays) },
	_ => true);
			_client = new MongoClient(connectionString);

			_db = _client.GetDatabase(database);
		}

		public List<T> Retrieve<T>(string collectionName, System.Linq.Expressions.Expression<Func<T, bool>> filter)
		{
			var col = _db.GetCollection<T>(collectionName);
			var task = col.Find(filter).ToListAsync();
			task.Wait();
			return task.Result;
		}

		public void Insert<T>(string collectionName, T data)
		{
			var col = _db.GetCollection<T>(collectionName);
			var task = col.InsertOneAsync(data);
			task.Wait();
		}
		public void InsertMany<T>(string collectionName, List<T> data)
		{
			var col = _db.GetCollection<T>(collectionName);
			var task = col.InsertManyAsync(data);
			task.Wait();
		}

		public void Replace<T>(string collectionName, System.Linq.Expressions.Expression<Func<T, bool>> filter, T replacement)
		{
			var col = _db.GetCollection<T>(collectionName);
			var task = col.ReplaceOneAsync<T>(filter, replacement);
			task.Wait();
		}

		public long Count<T>(string collectionName, System.Linq.Expressions.Expression<Func<T, bool>> filter)
		{
			var col = _db.GetCollection<T>(collectionName);
			var task = col.CountAsync<T>(filter);
			task.Wait();
			return task.Result;
		}

		public void InsertOrReplace<T>(string collectionName, System.Linq.Expressions.Expression<Func<T, bool>> filter, T obj, bool forceDelete = false)
		{
			if(Count<T>(collectionName, filter) > 0)
			{
				if(forceDelete)
				{
					Delete<T>(collectionName, filter);
					Insert<T>(collectionName, obj);
				}
				else
				{
					Replace<T>(collectionName, filter, obj);
				}
			}
			else
			{
				Insert<T>(collectionName, obj);
			}
		}

		public void Delete<T>(string collectionName, System.Linq.Expressions.Expression<Func<T, bool>> filter)
		{
			var col = _db.GetCollection<T>(collectionName);
			var task = col.DeleteManyAsync(x => true);
			task.Wait();
		}

		public void ClearCollection<T>(string collectionName)
		{
			var col = _db.GetCollection<T>(collectionName);
			col.DeleteManyAsync(x => true);
		}
	}
}
