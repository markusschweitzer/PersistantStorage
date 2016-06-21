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
            var connection = new PersistantStorageConnection();

            Exception connectException;
            if (!connection.Connect(@"mongodb://homeservice:qwert123@gate.homehub.io/homeservice", out connectException))
            {
                Console.WriteLine(string.Format("Cant't connect. Reason='{0}'", connectException.Message));
            }

            var stringList = new PersistantList<string>(connection, "homeservice", "string_demo");

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

            Console.WriteLine("======================================================");

            var stringDict = new PersistantDictionary<int, string>(connection, "homeservice", "dict_demo");

            count = stringDict.Count();
            Console.WriteLine("Count before clear: " + count);

            stringDict.Clear();

            count = stringDict.Count();
            Console.WriteLine("Count after clear: " + count);

            id = stringDict.Add(2, "testzwei");

            var dbkvp = stringDict.Get(id, true);
            Console.WriteLine("From db: " + dbkvp.KeyObject + " " + dbkvp.DataObject);

            using(var update = stringDict.CreateUpdateContext(id))
            {
                update.DataObject = "testzwei_new";
            }
            dbkvp = stringDict.Get(id, true);
            Console.WriteLine("From db: " + dbkvp.KeyObject + " " + dbkvp.DataObject);

            Console.WriteLine("======================================================");

            var myList = new PersistantDictionary<int, Device>(connection, "homeservice", "dev_demo");

            id = myList.GetId((x, y) => x == 15);
            var dev = myList.Get(id);

            var d2 = new Device();
            d2.name = "d2";
            d2.uid = "ddd";
            var id2 = myList.Add(18, d2);

            id = myList.GetId((x, y) => x == 18);
            var de2v = myList.Get(id2);
            Console.ReadLine();
        }
    }

    public class Device
    {
        public string name;
        public string uid;
    }
}
