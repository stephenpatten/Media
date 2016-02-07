using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaHub.ImdbLoader
{
	public static class DictionaryExtensions
	{
		public static void Insert<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
		{
			if (dictionary.ContainsKey(key)) dictionary[key] = value;
			else dictionary.Add(key, value);
		}

		public static bool InsertIf<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Func<TValue, bool> predicate, TKey key, TValue value)
		{
			if (dictionary.ContainsKey(key))
			{
				if (predicate(dictionary[key]) == true)
				{
					dictionary[key] = value;
					return true;
				}
			}
			else
			{
				dictionary.Add(key, value);
				return true;
			}

			return false;
		}
	}
}
