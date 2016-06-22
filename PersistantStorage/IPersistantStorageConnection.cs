using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorage
{
    public interface IPersistantStorageConnection
    {
        string DefaultDatabase { get; set; }

        bool IsConnected { get; }

        bool Connect(string connectionString, out Exception exception);

        void Disconnect();

        IMongoDatabase GetDatabase(string database);
        IMongoCollection<T> GetCollection<T>(IMongoDatabase database, string collection);
        IMongoCollection<T> GetCollection<T>(string database, string collection);

        IPersistantList<T> CreateList<T>(string database, string collection);
        IPersistantDictionary<K, T> CreateDictionary<K, T>(string database, string collection);

        IPersistantList<T> CreateListIfNull<T>(IPersistantList<T> list, string database, string collection);
        IPersistantDictionary<K, T> CreateDictionaryIfNull<K, T>(IPersistantDictionary<K, T> dict, string database, string collection);
    }
}
