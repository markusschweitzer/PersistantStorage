namespace PersistantStorage
{
	public interface IPersistantListElement
	{
		int Id
		{
			get;
		}
	}

	public class NameValue<TName, TValue>: IPersistantListElement
	{
		public TName Name;
		public TValue Value;
		public NameValue(TName key, TValue value)
		{
			Name = key;
			Value = value;
		}

		public int Id => Name.GetHashCode();
	}
}
