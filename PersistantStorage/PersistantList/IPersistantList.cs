using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorage
{
    public interface IPersistantList<T>
    {
        T Get(string id, bool forceDb = false);

        List<T> Get(Func<T, bool> filter);



        List<string> GetId(Func<T, bool> filter);

        



        void Clear();



        long Count();




        void Remove(string id);

        void Remove(List<string> ids);

        void Remove(Func<T, bool> filter);




        void Update(string id, Func<T, T> update);
        
        PersistantListUpdateContext<T> CreateUpdateContext(string id);




        void ForEach(Action<PersistantListElement<T>> action);

        void ForEachUpdate(Func<PersistantListElement<T>, PersistantListElement<T>> action);

        void ForEachElement(Action<T> action);

        void ForEachElementUpdate(Func<T, T> action);



        void ResetCollection(bool keepEntries);



        IReadOnlyList<PersistantListElement<T>> ToList();

        IReadOnlyList<T> ToElementList();

        PersistantListElement<T>[] ToArray();

        T[] ToElementArray();



        string Export(IPersistantSerializer serializer);
        void Import(IPersistantSerializer serializer, string data);

    }
}
