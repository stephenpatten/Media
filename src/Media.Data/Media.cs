using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raven.Imports.Newtonsoft.Json.Linq;

namespace MediaHub.Data
{
	public enum MediaType : uint { Unknown, Movie, TvSeries, Tv, Video, VideoGame }

	public class ImdbMedia
	{
		public ImdbMedia()
		{
		}

		public string Id { get; set; }
		public Imdb Imdb { get; set; }
	}

    public class Imdb
    {
		public Imdb()
		{
			YearIntervals = new List<YearInterval>();
			Genres = new HashSet<string>();
			Plots = new List<Plot>();
			AlsoKnownAs = new List<AlsoKnownAs>();
			ParentAlsoKnownAs = new List<AlsoKnownAs>();
		}

		public string ImdbFullTitle { get; set; }
		public string ImdbFullTitlePartial { get; set; }
		public string Title { get; set; }
		public int Year { get; set; }
		public string Numeral { get; set; }
		public List<YearInterval> YearIntervals { get; set; }
		public string EpisodeName { get; set; }
		public int? EpisodeAiredYear { get; set; }
		public int? EpisodeAiredMonth { get; set; }
		public int? EpisodeAiredDay { get; set; }
		public int? Season { get; set; }
		public int? Episode { get; set; }
		public MediaType Type { get; set; }
		public bool IsSuspended { get; set; }

		public HashSet<string> Genres { get; set; }
		public List<Plot> Plots { get; set; }
		public List<AlsoKnownAs> AlsoKnownAs { get; set; }
		public ImdbRating ImdbRating { get; set; }

		public string ParentMediaId { get; set; }
		public List<AlsoKnownAs> ParentAlsoKnownAs { get; set; }
	}

	public class YearInterval
	{
		public int? YearFrom { get; set; }
		public int? YearTo { get; set; }
	}

	public class Plot
	{
		public string Text { get; set; }
		public string By { get; set; }
	}

	public class AlsoKnownAs
	{
		public string Title { get; set; }
		public string Country { get; set; }
		public string Description { get; set; }
	}

	public class ImdbRating
	{
		public decimal Rank { get; set; }
		public int Votes { get; set; }
		public decimal Distribution1 { get; set; }
		public decimal Distribution2 { get; set; }
		public decimal Distribution3 { get; set; }
		public decimal Distribution4 { get; set; }
		public decimal Distribution5 { get; set; }
		public decimal Distribution6 { get; set; }
		public decimal Distribution7 { get; set; }
		public decimal Distribution8 { get; set; }
		public decimal Distribution9 { get; set; }
		public decimal Distribution10 { get; set; }
	}
}
