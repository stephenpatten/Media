using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaHub.ImdbLoader.Entities
{
	public class MovieBase
	{
		public enum MovieType : uint { Movie, TvSeries, Tv, Video, VideoGame }

		public MovieBase()
		{
			YearIntervals = new List<YearInterval>();
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
		public MovieType Type { get; set; }
		public bool IsSuspended { get; set; }
	}

	public class MediaIdBase
	{
		public MediaIdBase()
		{
		}

		public string ImdbFullTitle { get; set; }
		public string GlobalId { get; set; }
	}

    public class EpisodeBase : MovieBase
    {
    }

	public class AkaTitleBase
	{
		public AkaTitleBase()
		{
			AlsoKnownAs = new List<AkaTitleDefinition>();
		}

		public string ImdbFullTitle { get; set; }
		public List<AkaTitleDefinition> AlsoKnownAs { get; set; }
	}

	public class AkaTitleDefinition
	{
		public string ImdbFullTitle { get; set; }
		public string Title { get; set; }
		public string Country { get; set; }
		public string Description { get; set; }
	}

	public class RatingBase
	{
		public string Distribution { get; set; }
		public int Votes { get; set; }
		public decimal Rank { get; set; }
		public string ImdbFullTitle { get; set; }

		public decimal GetRatingPercent(int ratingValue)
		{
			if (Distribution.Length != 10) throw new ApplicationException("Distribution should always be 10 characters in length!");
			else if (ratingValue < 1 || ratingValue > 10) throw new ArgumentException("Rating value has to be in the range 1-10!");

			var c = Distribution.Substring(ratingValue - 1, 1);

			if (c == ".") return (decimal)0;
			else if (c == "*") return (decimal)1;
			else
			{
				var sum = Distribution.Sum(x => { var v = 0; if (x == '*') v = 1; else if (x != '.') v = Convert.ToInt32(x.ToString()); return v; });
				return (decimal)Convert.ToInt32(c) / (decimal)sum * (decimal)100;
			}
		}
	}

	public class GenreBase
	{
		public string ImdbFullTitle { get; set; }
		public string Genre { get; set; }
	}

    public class PlotBase
    {
        public PlotBase()
		{
			Plots = new List<PlotDefinition>();
		}

        public string ImdbFullTitle { get; set; }
        public List<PlotDefinition> Plots { get; set; }
    }

    public class PlotDefinition
    {
        public string Plot { get; set; }
        public string By { get; set; }
    }

	public class YearInterval
	{
		public int? YearFrom { get; set; }
		public int? YearTo { get; set; }
	}
}
