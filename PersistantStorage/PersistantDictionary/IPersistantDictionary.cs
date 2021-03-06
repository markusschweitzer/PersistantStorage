﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorage
{
    public interface IPersistantDictionary<K, T>
    {
        Task<string> AddAsync(K key, T item);

        string Add(K key, T item);

        string AddOrReplace(K key, T item);

        T this[K key] { get; }

        T Get(K key);

        bool TryGetValue(K key, out T value);

        bool ContainsKey(K key, out string id);

        bool ContainsValue(T value, out string id);

        string SetValue(K key, T value);

        PersistantDictionaryElement<K, T> Get(string id, bool forceDb = false);

        List<string> GetId(Func<K, T, bool> filter);

        T GetOrDefaultValue(K key, T defaultValue, bool save = false);



        void Clear();



        int Count();
        int Count(Func<PersistantDictionaryElement<K, T>, bool> filter);



        void Remove(string id);

        void Remove(List<string> ids);

        void Remove(Func<K, T, bool> filter);



        Task UpdateAsync(string id, Func<PersistantDictionaryElement<K, T>, PersistantDictionaryElement<K, T>> update);
        void Update(string id, Func<PersistantDictionaryElement<K, T>, PersistantDictionaryElement<K, T>> update);

        PersistantDictionaryUpdateContext<K, T> CreateAsyncUpdateContext(string id);
        PersistantDictionaryUpdateContext<K, T> CreateUpdateContext(string id);




        List<PersistantDictionaryElement<K, T>> ToList();

        Dictionary<K, T> ToDictionary();

        PersistantDictionaryElement<K, T>[] ToArray();




        void ForEachUpdate(Func<PersistantDictionaryElement<K, T>, PersistantDictionaryElement<K, T>> action);




        void ResetCollection(bool keepEntries);


        string Export(IPersistantSerializer serializer);
        void Import(IPersistantSerializer serializer, string data);

    }
}
