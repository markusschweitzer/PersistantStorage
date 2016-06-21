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

###PersistantList

To create a new ```PersistantList``` you can either call the constructor, or get a new one from the ```PersistantConnection``` object.

```csharp
var list = new PersistantList<string>(connection, "database", "collection");

var list2 = connection.CreateList<string>("database", "collection");
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
        IReadOnlyList<PersistantListElement<T>> ToList();
        IReadOnlyList<T> ToElementList();
        
        PersistantListElement<T>[] ToArray();
        T[] ToElementArray();
        
        List<string> GetId(Func<T, bool> filter);

        void Clear();

        long Count();

        void Remove(string id);
        void Remove(List<string> ids);
        void Remove(Func<T, bool> filter);
```
