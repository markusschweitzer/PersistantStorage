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
            var connection = new PersistantConnection();
            connection.Connect(@"mongodb://homeservice:qwert123@gate.homehub.io/homeservice", "homeservice");

            var stringList = new PersistantList<string>(connection, "stringDemo");
        }
    }
}
