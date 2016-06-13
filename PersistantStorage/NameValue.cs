using System;

namespace PersistantStorage
{
	public class NameValue: MarshalByRefObject
	{
		public string Name;
		public object Value;

		public NameValue(string name, object value)
		{
			Name = name;
			Value = value;
		}
	}
}
