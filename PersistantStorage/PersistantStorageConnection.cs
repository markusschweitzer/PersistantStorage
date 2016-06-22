using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorage
{
    public class PersistantStorageConnection : MarshalByRefObject, IPersistantStorageConnection
    {
        private MongoClient _client;

        public string DefaultDatabase
        {
            get;
            set;
        }

        public bool IsConnected
        {
            get
            {
                return _client != null;
            }
        }

        public bool Connect(string connectionString, out Exception exception)
        {
            exception = null;
            try
            {
                _client = new MongoClient(connectionString);

                ConventionRegistry.Register("DictionaryRepresentationConvention", new ConventionPack { new DictionaryRepresentationConvention(DictionaryRepresentation.ArrayOfArrays) },_ => true);
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                _client = null;
                return false;
            }
        }

        public bool Connect(string connectionString, string defaultDatabase, out Exception exception)
        {
            DefaultDatabase = defaultDatabase;

            return Connect(connectionString, out exception);
        }

        public void Disconnect()
        {
            _client = null;
        }


        public IMongoDatabase GetDatabase(string database)
        {
            if (IsConnected)
            {
                return _client.GetDatabase(database);
            }
            throw new InvalidOperationException("Client not connected!");
        }

        public IMongoCollection<T> GetCollection<T>(IMongoDatabase database, string collection)
        {
            if (IsConnected)
            {
                return database.GetCollection<T>(collection);
            }
            throw new InvalidOperationException("Client not connected!");
        }

        public IMongoCollection<T> GetCollection<T>(string database, string collection)
        {
            if (IsConnected)
            {
                return GetDatabase(database).GetCollection<T>(collection);
            }
            throw new InvalidOperationException("Client not connected!");
        }



        public IPersistantList<T> CreateList<T>(string collection, string database = null)
        {
            return new PersistantList<T>(this, database, collection);
        }

        public IPersistantDictionary<K, T> CreateDictionary<K, T>(string collection, string database = null)
        {
            return new PersistantDictionary<K, T>(this, database, collection);
        }



        public IPersistantList<T> CreateListIfNull<T>(IPersistantList<T> list, string collection, string database = null)
        {
            if (list == null)
            {
                return CreateList<T>(database, collection);
            }
            else
            {
                return list;
            }
        }

        public IPersistantDictionary<K, T> CreateDictionaryIfNull<K, T>(IPersistantDictionary<K, T> dict, string collection, string database = null)
        {
            if (dict == null)
            {
                return CreateDictionary<K, T>(database, collection);
            }
            else
            {
                return dict;
            }
        }
    }
}
