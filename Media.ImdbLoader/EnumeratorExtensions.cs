using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaHub.ImdbLoader
{
	public static class EnumeratorExtensions
	{
		public static IEnumerable<TElement> TakeWhile<TElement>(this IEnumerator<TElement> self, Func<TElement, bool> predicate)
		{
			while (predicate(self.Current))
			{
				yield return self.Current;

				if (!self.MoveNext()) yield break;
			}
		}

		public static IEnumerator<TElement> SkipWhile<TElement>(this IEnumerator<TElement> self, Func<TElement, bool> predicate, out List<TElement> skipped)
		{
			List<TElement> s = new List<TElement>();
			if (self.Current != null)
			{
				while (predicate(self.Current))
				{
					s.Add(self.Current);
					if (!self.MoveNext()) break;
				}
			}
			skipped = s;
			return self;
		}
	}
}
