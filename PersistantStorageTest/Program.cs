using PersistantStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorageTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var stringList = new PersistantList<string>(@"mongodb://homeservice:qwert123@gate.homehub.io/homeservice", "homeservice", "string_demo");

            long count = stringList.Count();
            Console.WriteLine("Count before clear: " + count);

            stringList.Clear();

            count = stringList.Count();
            Console.WriteLine("Count after clear: " + count);


            string localString = "dasisteinneuerstring";
            Console.WriteLine("Local string: " + localString);

            var id = stringList.Add(localString);
            Console.WriteLine("Db id: " + id.ToString());

            var dbString = stringList.Get(id);
            Console.WriteLine("Retrieved string: " + dbString);

            localString += "_add";
            Console.WriteLine("Updated local string: " + localString);

            stringList.Update(id, x => x += "_add");

            dbString = stringList.Get(id);
            Console.WriteLine("Retrieved updated string: " + dbString);


            using(var updateContext = stringList.CreateUpdateContext(id))
            {
                updateContext.DataObject = "newupdatedContextString";
            }

            dbString = stringList.Get(id);
            Console.WriteLine("Retrieved updated context string: " + dbString);

            Console.WriteLine("ToList elements:");
            foreach(var ele in stringList.ToList())
            {
                Console.WriteLine(ele.DataObject);
            }

            Console.WriteLine("Reset collection");

            stringList.ResetCollection(true);

            dbString = stringList.Get(id);
            Console.WriteLine("Retrieved reseted elemenr string: " + dbString);


            Console.WriteLine("ForEach:");
            stringList.ForEach(x => {
                Console.WriteLine("Id: " + x.Id + " Data: " + x.DataObject);
            });

            Console.WriteLine("ForEachElement:");
            stringList.ForEachElement(x => {
                Console.WriteLine("Data: " + x);
            });

            stringList.ForEachElementUpdate(x => x += "_2nd");

            Console.WriteLine("ForEach IEnumerable loop:");
            foreach(var x in stringList)
            {
                Console.WriteLine("Id: " + x.Id + " Data: " + x.DataObject);
            }


            Console.ReadLine();
        }
    }
}
