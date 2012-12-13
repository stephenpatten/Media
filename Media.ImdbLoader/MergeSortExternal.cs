using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaHub.ImdbLoader
{
	public class MergeSortExternalDefinition<TItem>
		where TItem: class
	{
		public string InPath { get; set; }
		public string OutPath { get; set; }
		public string ItemSeparator { get; set; }
		public string StartAt { get; set; }
		public string EndAt { get; set; }
		public int ChunkSize { get; set; }
		public Func<byte[], MergeSortExternalDefinition<TItem>, TItem> ReadItem { get; set; }
		public Func<TItem, byte[]> WriteItem { get; set; }
		public IComparer<TItem> Comparer { get; set; }
		public Encoding Enc { get; set; }
	}

	public static class MergeSortExternal
	{
		public static void Run<TItem>(MergeSortExternalDefinition<TItem> def)
			where TItem: class
		{
			var st = Stopwatch.StartNew();

			var parts = new List<string>();

			var itemSeparatorBytes = def.Enc.GetBytes(def.ItemSeparator);
			var startBytes = def.StartAt != null ? def.Enc.GetBytes(def.StartAt) : null;
			var endBytes = def.EndAt != null ? def.Enc.GetBytes(def.EndAt) : null;

			var startIndex = def.StartAt != null ? -1 : 0;
			var endIndex = def.EndAt != null ? -1 : 0;
			var currentDataOffset = 0;
			var currentEndOffset = 0;

			byte[] overlap = null;

			var chunks = GetDataChunks(def.InPath, def.ChunkSize).GetEnumerator();
			if (!chunks.MoveNext()) return;
			while (true)
			{
				var chunk = chunks.Current;
				var isFinalChunk = !chunks.MoveNext();

				byte[] data = overlap != null ? overlap.Concat(chunk).ToArray() : chunk;
				overlap = null;

				if (startIndex == -1)
				{
					startIndex = data.IndexOfSequence(startBytes).DefaultIfEmpty(-1).First();
					if (startIndex == -1)
					{
						overlap = new byte[startBytes.Length - 1];
						Buffer.BlockCopy(data, data.Length - overlap.Length, overlap, 0, overlap.Length);
						continue;
					}
					else
					{
						startIndex += startBytes.Length;
						data = data.Range(startIndex, data.Length - 1);
						overlap = null;
					}
				}
				
				if (endIndex == -1)
				{
					endIndex = data.IndexOfSequence(endBytes, currentEndOffset).DefaultIfEmpty(-1).First();
					if (endIndex == -1)
					{
						overlap = new byte[endBytes.Length - 1];
						Buffer.BlockCopy(data, data.Length - overlap.Length, overlap, 0, overlap.Length);
					}
					else
					{
						endIndex -= 1;
						data = data.Range(0, endIndex);
						overlap = null;
						isFinalChunk = true;
					}
				}

				//get items
				int lastLineOffset = 0;
				var lines = data.FromSeparator(itemSeparatorBytes, out lastLineOffset, currentDataOffset);
				var items = lines.Select(x => def.ReadItem(x, def))
					.ToList();

				if (overlap == null || overlap.Length < data.Length - lastLineOffset)
				{
					currentDataOffset = 0;
					currentEndOffset = overlap != null ? (data.Length - lastLineOffset) - overlap.Length : 0;

					if (data.Length - lastLineOffset > 0)
					{
						overlap = new byte[data.Length - lastLineOffset];
						Buffer.BlockCopy(data, data.Length - overlap.Length, overlap, 0, overlap.Length);

						if (isFinalChunk)
						{
							items.Add(def.ReadItem(overlap, def));
						}
					}
				}
				else
				{
					currentDataOffset = overlap.Length - (data.Length - lastLineOffset);
					currentEndOffset = 0;
				}

				items.Sort(def.Comparer);

				var tempfile = Path.GetTempFileName();
				using (var fo = File.Open(tempfile, FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					for (var l = 0; l < items.Count; l++)
					{
						var lineData = def.WriteItem(items[l]);
						fo.Write(lineData, 0, lineData.Length);
					}
				}
				parts.Add(tempfile);

				if (isFinalChunk) break;
			}

			//put all tempfiles together
			using (var fo = File.Open(def.OutPath, FileMode.Create, FileAccess.Write, FileShare.Read))
			{
				var prioQueue = new SortedNonUniqueList<FileStreamItem<TItem>>(new PropertyComparer<FileStreamItem<TItem>, TItem>(def.Comparer, x => x.Item));
				List<FileStream> partialfiles = null;
				try
				{
					partialfiles = parts.Select(x => File.Open(x, FileMode.Open, FileAccess.Read, FileShare.Read)).ToList();

					partialfiles.ForEach(x =>
					{
						var bytes = ReadUntil(x, itemSeparatorBytes);
						if (bytes != null)
						{
							var item = def.ReadItem(bytes, def);
							if (item != null) prioQueue.Add(new FileStreamItem<TItem>(item, x));
						}
					});

					while (prioQueue.Count > 0)
					{
						var fsi = prioQueue.PopFirst();

						var lineData = def.WriteItem(fsi.Item);
						fo.Write(lineData, 0, lineData.Length);

						var bytes = ReadUntil(fsi.Fi, itemSeparatorBytes);
						if (bytes != null)
						{
							var fso = new FileStreamItem<TItem>(def.ReadItem(bytes, def), fsi.Fi);
							if (fso.Item != null) prioQueue.Add(fso);
						}
					}
				}
				finally
				{
					if (partialfiles != null)
					{
						partialfiles.ForEach(x => x.Dispose());
						partialfiles.Clear();
						parts.ForEach(x => File.Delete(x));
					}
				}
			}
		}

		private class SortedNonUniqueList<TElement> : List<TElement>
			where TElement: class
		{
			private IComparer<TElement> comparer;

			public SortedNonUniqueList(IComparer<TElement> comparer)
			{
				this.comparer = comparer;
			}

			public new void Add(TElement item)
			{
				base.Add(item);
				base.Sort(comparer);
			}

			public TElement PopFirst()
			{
				var element = this.ElementAt(0);
				this.RemoveAt(0);

				return element;
			}
		}

		private static byte[] ReadUntil(FileStream fi, byte[] itemSeparatorBytes)
		{
			var bufferSize = 128;
			var buffer = new byte[bufferSize];
			int n = 0, tot = 0, offset = -1;
			while ((n = fi.Read(buffer, tot, bufferSize)) > 0)
			{
				tot += n;

				offset = buffer.IndexOfSequence(itemSeparatorBytes).DefaultIfEmpty(-1).First();
				if (offset == -1)
				{
					offset = -1;
					Array.Resize(ref buffer, buffer.Length + bufferSize);
					continue;
				}
				else if (offset + itemSeparatorBytes.Length != tot)
				{
					fi.Seek(offset + itemSeparatorBytes.Length - tot, SeekOrigin.Current);
				}

				break;
			}

			return offset == -1 ? null : buffer.Range(0, offset - 1);
		}

		private static IEnumerable<byte[]> GetDataChunks(string path, int chunkSize = 1024 * 1024 * 1)
		{
			var buffer = new byte[chunkSize];
			int n = 0, tot = 0;
			using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				while ((n = fs.Read(buffer, 0, buffer.Length)) > 0)
				{
					tot += n;
					yield return buffer.Range(0, n - 1);
				}
			}
		}
	}

	public class OrdinalByteArrayComparer<TItem> : IComparer<TItem>
		where TItem: class
	{
		private Func<TItem, byte[]> predicate;

		public OrdinalByteArrayComparer(Func<TItem, byte[]> predicate)
		{
			this.predicate = predicate;
		}

		public int Compare(TItem l, TItem r)
		{
			var x = predicate(l);
			var y = predicate(r);

			for (var i = 0; i < (x.Length > y.Length ? y.Length : x.Length); i++)
			{
				int comp = x[i].CompareTo(y[i]);
				if (comp != 0) return comp;
			}

			return x.Length == y.Length ? 0 : x.Length > y.Length ? 1 : -1;
		}
	}

	public class PropertyComparer<TItem, TReduce> : IComparer<TItem>
		where TItem: class
		where TReduce: class
	{
		private IComparer<TReduce> comparer;
		private Func<TItem, TReduce> predicate;

		public PropertyComparer(IComparer<TReduce> comparer, Func<TItem, TReduce> predicate)
		{
			this.comparer = comparer;
			this.predicate = predicate;
		}

		public int Compare(TItem x, TItem y)
		{
			return comparer.Compare(predicate(x), predicate(y));
		}
	}

	public class FileStreamItem<TItem>
		where TItem: class
	{
		public TItem Item { get; set; }
		public FileStream Fi { get; set; }

		public FileStreamItem(TItem item, FileStream fi)
		{
			Item = item;
			Fi = fi;
		}
	}
}
