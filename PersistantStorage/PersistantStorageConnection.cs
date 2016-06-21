using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorage
{
    public class PersistantStorageConnection
    {
        private MongoClient _client;

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
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                _client = null;
                return false;
            }
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

        public IPersistantList<T> CreateList<T>(string database, string collection)
        {
            return new PersistantList<T>(this, database, collection);
        }

        public IPersistantDictionary<K, T> CreateDictionary<K, T>(string database, string collection)
        {
            return new PersistantDictionary<K, T>(this, database, collection);
        }
    }
}
