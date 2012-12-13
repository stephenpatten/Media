using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MediaHub.ImdbLoader
{
	public static class ByteExtensions
	{
		public static IEnumerable<int> IndexOfSequence(this byte[] buffer, byte[] pattern, int startIndex = 0, int endIndex = -1)
		{
			if (endIndex == -1) endIndex = buffer.Length - 1;

			int i = Array.IndexOf<byte>(buffer, pattern[0], startIndex, endIndex - startIndex + 1);
			while (i >= 0 && i <= endIndex - pattern.Length + 1)
			{
				byte[] segment = new byte[pattern.Length];
				Buffer.BlockCopy(buffer, i, segment, 0, pattern.Length);
				if (segment.SequenceEqual<byte>(pattern))
				{
					yield return i;
					i = Array.IndexOf<byte>(buffer, pattern[0], i + pattern.Length, endIndex - (i + pattern.Length) + 1);
				}
				else
					i = Array.IndexOf<byte>(buffer, pattern[0], i + 1, endIndex - i);
			}
		}

		public static List<byte[]> FromSeparator(this byte[] buffer, byte[] pattern, out int lastOffset, int startIndex = 0, int endIndex = -1)
		{
			var results = new List<byte[]>();

			var lastIndex = startIndex;
			foreach (var i in IndexOfSequence(buffer, pattern, startIndex, endIndex))
			{
				byte[] segment = new byte[i - lastIndex];
				Buffer.BlockCopy(buffer, lastIndex, segment, 0, i - lastIndex);

				results.Add(segment);
				lastIndex = i + pattern.Length;
			}

			lastOffset = lastIndex;
			return results;
		}

		public static List<byte[]> Split(this byte[] buffer, byte[] pattern, int startIndex = 0, int endIndex = -1)
		{
			var results = new List<byte[]>();

			var lastIndex = startIndex;
			foreach (var i in IndexOfSequence(buffer, pattern, startIndex, endIndex))
			{
				byte[] segment = new byte[i - lastIndex];
				Buffer.BlockCopy(buffer, lastIndex, segment, 0, i - lastIndex);

				results.Add(segment);
				lastIndex = i + pattern.Length;
			}

			if (lastIndex < buffer.Length - 1)
			{
				byte[] segment = new byte[buffer.Length - lastIndex];
				Buffer.BlockCopy(buffer, lastIndex, segment, 0, segment.Length);

				results.Add(segment);
			}

			return results;
		}

		public static byte[] Range(this byte[] buffer, int startIndex, int endIndex)
		{
			var len = endIndex - startIndex + 1;
			if (buffer.Length == 0 || len < 1) return null;

			byte[] segment = new byte[len];
			Buffer.BlockCopy(buffer, startIndex, segment, 0, len);

			return segment;
		}

		public static byte[] ConcatAll(this byte[] self, params byte[][] args)
		{
			var len = self.Length + args.Select(x => x.Length).Sum();
			var at = self.Length;

			var buffer = new byte[len];

			Buffer.BlockCopy(self, 0, buffer, 0, self.Length);
			foreach (var arg in args)
			{
				Buffer.BlockCopy(arg, 0, buffer, at, arg.Length);
				at += arg.Length;
			}

			return buffer;
		}

		public static byte[] Join(this IList<byte[]> self, byte[] separator)
		{
			var len = self.Select(x => x.Length).Sum() + (separator.Length * self.Count - 1);
			var at = 0;

			var buffer = new byte[len];

			for (var i = 0; i < self.Count; i++)
			{
				var item = self[i];

				Buffer.BlockCopy(item, 0, buffer, at, item.Length);
				at += item.Length;

				if (i < self.Count - 1)
				{
					Buffer.BlockCopy(separator, 0, buffer, at, separator.Length);
					at += separator.Length;
				}
			}

			return buffer;
		}

		public static byte[] Repeat(this byte[] self, int count)
		{
			var len = self.Length * count;
			var at = 0;

			var buffer = new byte[len];

			for (var i = 0; i < count; i++)
			{
				Buffer.BlockCopy(self, 0, buffer, at, self.Length);
				at += self.Length;
			}

			return buffer;
		}
	}
}
