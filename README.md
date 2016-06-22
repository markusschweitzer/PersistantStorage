# PersistantStorage
Persistant .NET collections backed by MongoDB

This collections are not meant to be high performance, they should be easy to use for applications with small to medium data.

###PersistantConnection

In order to create persistant elements, you need a connection to your DB server first.

```csharp
var connection = new PersistantStorageConnection();

Exception connectException;
if (!connection.Connect(@"mongodb://someconnectionstring@server/soemthingelse", out connectException))
{
    Console.WriteLine(string.Format("Cant't connect to db. Reason='{0}'", connectException.Message));
}
```
You can check the connection state by using the ```IsConnected``` property:

```csharp
if (!connection.IsConnected)
{
...
}
```

To create persistant elements you need either the name of the database and collection, or the MongoDB object. Therefore ```PersistantConnection``` has some usefull methods:
```csharp
IMongoDatabase GetDatabase(string database);
IMongoCollection<T> GetCollection<T>(IMongoDatabase database, string collection);
IMongoCollection<T> GetCollection<T>(string database, string collection);
```

```PersistantConnection``` has a ```DefaultDatabase``` property. If you specify it, the database name in the constructor and create calls can be omited.

```csharp
connection.DefaultDatabase = "database";
```

###PersistantList

To create a new ```PersistantList``` you can either call the constructor, or get a new one from the ```PersistantConnection``` object.

```csharp
var list = new PersistantList<string>(connection, "collection", "database");

var list2 = connection.CreateList<string>("collection", "database");
```

Afterwards, you can use the list to store objects in it:

```csharp
string id = list.Add("aString");
```
Foreach ```Add``` call, you will get a new unique database id returned. This id can be used get exactly this element from the db back.

```csharp
string element = list.Get(id);
```
Or fetch elements by ```Func``` or lambda expression:
```csharp
List<string> elements = list.Get(x => x.Contains("test"));
```
Loops:
```csharp
list.ForEachElement(x => Console.WriteLine(x));

foreach(string s in list)
{

}
```

But remember, that each object returned by the persistant list is just a local copy. Changes won't be saved in the DB.
To update a item (locally AND in the db), a update context is needed (which takes the database id as string).
```updateContext.DataObject``` will reflect the current element to update. Changes on this object will be saved in the database.

```csharp
using(var updateContext = stringList.CreateUpdateContext(id))
{
    updateContext.DataObject = "new string";
}
```

For easy updating of multiple items a convinient ForEach loop is provided:

```csharp
list.ForEachElementUpdate(x => x += "addition");
```

Also for better handling, a few support methods are provided:

```csharp
List<PersistantListElement<T>> ToList();
List<T> ToElementList();

PersistantListElement<T>[] ToArray();
T[] ToElementArray();

List<string> GetId(Func<T, bool> filter);

void Clear();

long Count();

void Remove(string id);
void Remove(List<string> ids);
void Remove(Func<T, bool> filter);
```

It is possible to export and import the whohle list to/from a string. To use this feature you need to implement the IPersistantSerializer interface with your custom string serializer:

```csharp
    class MyStringSerializer : IPersistantSerializer
    {
        public T Deserialize<T>(string data)
        {
            //add your deserialization code here
        }

        public string Serialize<T>(T data)
        {
            //add your serialization code here
        }
    }
```
Afterwards you can call the import/export methods of PersistantElements:

```csharp

var list = new PersistantList<string>(connection, "collection", "database");
list.Add("aString");

string exportedList = list.Export(new MyStringSerializer());
```

###PersistantDictionary

To create a new ```PersistantDictionary``` you can either call the constructor, or get a new one from the ```PersistantConnection``` object.

```csharp
var dict = new PersistantDictionary<int, string>(connection, "collection", "database");

var dict2 = connection.CreateDictionary<int, string>("collection", "database");
```

Afterwards, you can use the dictionary to store objects in it:

```csharp
string id = dict.Add(15, "aString");
```
Foreach ```Add``` call, you will get a new unique database id returned. This id can be used get exactly this key value pair from the db back.
```csharp
List<string> ids = dict.GetId((x, y) => x == 15)
```
Retrieving elements:

```csharp
var value = dict.Get(15);

string value;
if(!dict.TryGetValue(15, out value))
{
...
}

string defaultValue = "nothing";
string value = dict.GetOrDefaultValue(15, defaultvalue, false);
```

For fast checking if a key or value is present, the following methods are provided:

```csharp
bool ContainsKey(K key, out string id);

bool ContainsValue(T value, out string id);
```

But remember, that each object returned by the persistant list is just a local copy. Changes won't be saved in the DB.
To update a item (locally AND in the db), a update context is needed (which takes the database id as string).
```updateContext.DataObject``` will reflect the current element to update. Changes on this object will be saved in the database.

```csharp
using(var updateContext = dict.CreateUpdateContext(id))
{
    updateContext.KeyObject = 12;
    updateContext.DataObject = "newData";
}
```

For easy updating of multiple items a convinient ForEach loop is provided:

```csharp
list.ForEachUpdate(x => {
   x.DataObject = "newData";
   return x;
});
```

Also for better handling, a few support methods are provided:

```csharp
void Clear();

long Count();

void Remove(string id);
void Remove(List<string> ids);
void Remove(Func<K, T, bool> filter);

List<PersistantDictionaryElement<K, T>> ToList();

Dictionary<K, T> ToDictionary();

PersistantDictionaryElement<K, T>[] ToArray();
```
It is possible to export and import the whohle dictionary to/from a string. To use this feature you need to implement the IPersistantSerializer interface with your custom string serializer:

```csharp
    class MyStringSerializer : IPersistantSerializer
    {
        public T Deserialize<T>(string data)
        {
            //add your deserialization code here
        }

        public string Serialize<T>(T data)
        {
            //add your serialization code here
        }
    }
```
Afterwards you can call the import/export methods of PersistantElements:

```csharp

var dict = new PersistantDictionary<int, string>(connection, "collection", "database");
dict.Add(15, "aString");

string exportedDict = dict.Export(new MyStringSerializer());
```
