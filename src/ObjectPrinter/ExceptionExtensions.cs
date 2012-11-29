using System;
using ObjectPrinter.Utilties;

namespace ObjectPrinter
{
	public static class ExceptionExtensions
	{
		public const string CacheKey = "Cached_DetailedToString";

		///<summary>
		/// Uses the ObjectPrinter to loop through the properties of the object, dumping them to a string.  
		/// Includes special formatting to put all exception properties before the Exception.Data contents.
		/// Caches object printer output in the exception so repetative calls are cheap, in case the output gets logged more than once.
		/// </summary>
		public static string DumpToString(this Exception e)
        {
            return GetCachedValue(e, e.Dump());
		}

		///<summary>
		/// Uses the ObjectPrinter to loop through the properties of the object, dumping them to a string.  
        /// Includes special formatting to put all exception properties before the Exception.Data contents.
        /// Caches object printer output in the exception so repetative calls are cheap, in case the output gets logged more than once.
		/// </summary>
		public static string DumpToString(this Exception e, IObjectPrinterConfig config)
		{
		    return GetCachedValue(e, e.Dump(config));
		}

	    private static string GetCachedValue(Exception e, LazyString ls)
	    {
	        if (!e.Data.Contains(CacheKey))
	        {
	            var output = ls.ToString();
	            e.Data[CacheKey] = output;
	        }
	        return (string) e.Data[CacheKey];
	    }

	    public static void SetContext(this Exception e, string name, object context, bool ifNotSerializablePrintOnSerialize = false)
		{
			if (!context.GetType().IsSerializable)
			{
				context = new NonSerializableWrapper {Context = context, PrintOnSerialize = ifNotSerializablePrintOnSerialize};
			}
			e.Data[name] = context;
		}
	}
}