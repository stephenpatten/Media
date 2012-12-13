using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaHub.ImdbLoader
{
	public abstract class ByteEntity<TItem>
		where TItem: class
	{
		protected static byte[] Newline = new byte[] { 10 };
		protected static byte[] Tab = new byte[] { 9 };
		protected byte[] ItemSeparator
		{
			get
			{
				return Def.Enc.GetBytes(Def.ItemSeparator);
			}
		}

		public MergeSortExternalDefinition<TItem> Def { get; set; }

		public ByteEntity(MergeSortExternalDefinition<TItem> def)
		{
			Def = def;
		}

		public abstract byte[] GetBytes();
	}

	public class ByteMediaId : ByteEntity<ByteMediaId>
	{
		public byte[] FullImdbTitle { get; set; }
		public byte[] GlobalId { get; set; }

		public ByteMediaId(byte[] bytes, MergeSortExternalDefinition<ByteMediaId> def)
			: base(def)
		{
			FullImdbTitle = bytes.Range(0, Array.IndexOf(bytes, (byte)9) - 1);
			GlobalId = bytes.Range(Array.LastIndexOf(bytes, (byte)9) + 1, bytes.Length - 1);
		}

		public override byte[] GetBytes()
		{
			return FullImdbTitle.ConcatAll(Tab.Repeat(3), GlobalId, ItemSeparator);
		}
	}

	public class ByteMovie : ByteEntity<ByteMovie>
	{
		public byte[] Id { get; set; }
		public byte[] Extra { get; set; }

		public ByteMovie(byte[] bytes, MergeSortExternalDefinition<ByteMovie> def)
			: base(def)
		{
			Id = bytes.Range(0, Array.IndexOf(bytes, (byte)9) - 1);
			Extra = bytes.Range(Array.LastIndexOf(bytes, (byte)9) + 1, bytes.Length - 1);
		}

		public override byte[] GetBytes()
		{
			return Id.ConcatAll(Tab.Repeat(3), Extra, ItemSeparator);
		}
	}

	public class ByteGenre : ByteEntity<ByteGenre>
	{
		public byte[] Id { get; set; }
		public byte[] Genre { get; set; }

		public ByteGenre(byte[] bytes, MergeSortExternalDefinition<ByteGenre> def) : base(def)
		{
			Id = bytes.Range(0, Array.IndexOf(bytes, (byte)9) - 1);
			Genre = bytes.Range(Array.LastIndexOf(bytes, (byte)9) + 1, bytes.Length - 1);
		}

		public override byte[] GetBytes()
		{
			return Id.ConcatAll(Tab.Repeat(3), Genre, ItemSeparator);
		}
	}

	public class ByteAlsoKnownAs : ByteEntity<ByteAlsoKnownAs>
	{
		public byte[] Id { get; set; }
		public List<byte[]> Titles { get; set; }

		public ByteAlsoKnownAs(byte[] bytes, MergeSortExternalDefinition<ByteAlsoKnownAs> def)
			: base(def)
		{
			var lines = bytes.Split(Newline);

			Id = lines.FirstOrDefault();
			Titles = lines.Skip(1).ToList();
		}

		public override byte[] GetBytes()
		{
			return Id.ConcatAll(Newline, Titles.Join(Newline), ItemSeparator);
		}
	}

	public class ByteRating : ByteEntity<ByteRating>
	{
		public byte[] Id { get; set; }
		public byte[] Data { get; set; }

		public ByteRating(byte[] bytes, MergeSortExternalDefinition<ByteRating> def)
			: base(def)
		{
			Id = bytes.Range(32, bytes.Length - 1);
			Data = bytes.Range(0, 31);
		}

		public override byte[] GetBytes()
		{
			return Data.ConcatAll(Id, ItemSeparator);
		}
	}

	public class BytePlot : ByteEntity<BytePlot>
	{
		public byte[] Extra { get; set; }
		public byte[] Id { get; set; }
		public byte[] Data { get; set; }

		public BytePlot(byte[] bytes, MergeSortExternalDefinition<BytePlot> def)
			: base(def)
		{
			Extra = bytes.Range(0, 3);
			Id = bytes.Range(4, Array.IndexOf(bytes, (byte)10) - 1);
			Data = bytes.Range(Array.IndexOf(bytes, (byte)10) + 2, bytes.Length - 1);
		}

		public override byte[] GetBytes()
		{
			return Extra.ConcatAll(Id, Newline.Repeat(2), Data, ItemSeparator);
		}
	}
}
