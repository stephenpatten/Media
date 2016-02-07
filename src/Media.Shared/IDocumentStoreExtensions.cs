using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raven.Client;

namespace MediaHub.Shared
{
	public static class IDocumentStoreExtensions
	{
		public static void WaitForIndexing(this IDocumentStore self, Action<string[], TimeSpan> progressCallback = null)
		{
			var start = DateTime.UtcNow;
			var indexingTask = Task.Run(() =>
			{
				while (true)
				{
					var s = self.DatabaseCommands.GetStatistics().StaleIndexes;
					if (s.Length > 0)
					{
						if(progressCallback != null) progressCallback(s, (DateTime.UtcNow - start));
						Task.Delay(1000).Wait();
					}
					else break;
				}
			});

			indexingTask.Wait();
		}
	}
}
