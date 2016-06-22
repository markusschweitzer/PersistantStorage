using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorage
{
    public interface IPersistantSerializer
    {
        string Serialize<T>(T data);

        T Deserialize<T>(string data);
    }
}
