using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorage
{
    public class PersistantList<T>
    {
        private PersistantConnection _connection;
        private string _name;
        private List<T> _localCache;

        public PersistantList(PersistantConnection connection, string name)
        {
            _connection = connection;
            _name = name;
            _localCache = new List<T>();
            if (!_connection.Connected)
            {
                throw new ArgumentException("Supply a connected PersistantConnection.");
            }
        }

        public T GetOne(Func<T, bool> filter)
        {
            return _localCache.FirstOrDefault(filter);
        }
        public IEnumerable<T> Get(Func<T, bool> filter)
        {
            return _localCache.Where(filter);
        }

        public void Replace(T item, Func<T, bool> filter)
        {
            
        }

        public void Add(T item)
        {
            _localCache.Add(item);
            _connection.Insert(_name, new PersistantListElement<T>(item));
        }

        public void Remove(T item)
        {
            _localCache.Remove(item);
            _connection.Delete<PersistantListElement<T>>(_name, x => x.Data.Equals(item));
        }

        public long Count()
        {
            return _localCache.Count;
        }

        public bool Contains(T item)
        {
            return _localCache.Contains(item);
        }

        public List<T> GetList()
        {
            return _localCache;
        }
    }
}
