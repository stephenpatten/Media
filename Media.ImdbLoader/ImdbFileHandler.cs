using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MediaHub.ImdbLoader.Entities;
using System.IO;
using System.Diagnostics;
using System.Globalization;

namespace MediaHub.ImdbLoader
{
	public static class ImdbFileHandlerFactory
	{
		public static IImdbFileHandler<T> Create<T>(string data) where T: class, new()
		{
			if (typeof(T) == typeof(MovieBase))
			{
				return (IImdbFileHandler<T>)new ImdbFileHandler_Movies(data);
			}
			else if (typeof(T) == typeof(MediaIdBase))
			{
				return (IImdbFileHandler<T>)new ImdbFileHandler_MediaIds(data);
			}
			else if (typeof(T) == typeof(AkaTitleBase))
			{
				return (IImdbFileHandler<T>)new ImdbFileHandler_AkaTitles(data);
			}
            else if (typeof(T) == typeof(GenreBase))
            {
                return (IImdbFileHandler<T>)new ImdbFileHandler_Genres(data);
            }
            else if (typeof(T) == typeof(PlotBase))
            {
                return (IImdbFileHandler<T>)new ImdbFileHandler_Plots(data);
            }
            else if (typeof(T) == typeof(RatingBase))
            {
                return (IImdbFileHandler<T>)new ImdbFileHandler_Ratings(data);
            }
            else throw new ArgumentException(string.Format(@"Unsupported template class '{0}'!", typeof(T).Name));
		}
	}

	public interface IImdbFileHandler<T> where T : class
	{
		IEnumerable<T> GetParsedItems();
	}

	class ImdbFileHandler_Movies : ImdbFileHandler<MovieBase>
	{
		protected Regex r_title { get; set; }
		protected Regex r_yearInterval { get; set; }

		public ImdbFileHandler_Movies(string data)
			: base(data)
		{
			//StartDefinition = new Regex(@"\-{25,}\s*\n+MOVIES\s+LIST\s*\n+===========\s*\n+", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
			//EndDefinition = new Regex(@"\n+\-{25,}\s*(?:\n+|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
			StartDefinition = null;
			EndDefinition = null;
			ItemDefinition = new Regex(@"(?:^|\n+)\t*(?<line>[^\n]+?)\s*(?=\n+)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

			r_title = new Regex(@"^\t*   " + ImdbFullTitleDefinition + @"   (?:\s+(?:,?(?<yearInterval>(?:[0-9]{4,4}|\?\?\?\?)(?:-(?:[0-9]{4,4}|\?\?\?\?))?))+)?   (?:\s+\((?<comment>.+?)\))?   \s*$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
			r_yearInterval = new Regex(@"^   (?<yearFrom>[0-9]{4,4}|\?\?\?\?)   (?:-(?<yearTo>[0-9]{4,4}|\?\?\?\?))?   $", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

			GetDataOffset(out _offsetStart, out _offsetEnd);
		}

		protected override MovieBase ParseItem(Match item)
		{
			var match = r_title.Match(item.Groups["line"].Value);
			if (match.Success)
			{
				var movie = new MovieBase();
				bool isTvSeries = match.Groups["title"].Value.First() == '"' && match.Groups["title"].Value.Last() == '"';
				movie.ImdbFullTitle = match.Groups["fullTitle"].Value;
                movie.ImdbFullTitlePartial = match.Groups["fullTitlePartial"].Value;
				movie.Title = isTvSeries ? match.Groups["title"].Value.Substring(1, match.Groups["title"].Value.Length - 2) : match.Groups["title"].Value;
				movie.Year = match.Groups["year"].Success ? match.Groups["year"].Value.Equals("????", StringComparison.OrdinalIgnoreCase) ? -1 : int.Parse(match.Groups["year"].Value) : -1; //int.Parse(match.Groups["year"].Value);
				movie.Numeral = match.Groups["numeral"].Success ? match.Groups["numeral"].Value : null;
				movie.EpisodeName = match.Groups["episodeName"].Success ? match.Groups["episodeName"].Value : null;
                if (movie.EpisodeName != null)
                {

                }
                movie.EpisodeAiredYear = match.Groups["episodeYear"].Success ? int.Parse(match.Groups["episodeYear"].Value) : (int?)null;
                movie.EpisodeAiredMonth = match.Groups["episodeMonth"].Success ? int.Parse(match.Groups["episodeMonth"].Value) : (int?)null;
                movie.EpisodeAiredDay = match.Groups["episodeDay"].Success ? int.Parse(match.Groups["episodeDay"].Value) : (int?)null;
				movie.Season = match.Groups["season"].Success ? int.Parse(match.Groups["season"].Value) : (int?)null;
				movie.Episode = match.Groups["episode"].Success ? int.Parse(match.Groups["episode"].Value) : (int?)null;

				movie.Type = MovieBase.MovieType.Movie;
				if (isTvSeries) movie.Type = MovieBase.MovieType.TvSeries;
				else if (match.Groups["isTvSeries"].Success) movie.Type = MovieBase.MovieType.Tv;
				else if (match.Groups["isVideo"].Success) movie.Type = MovieBase.MovieType.Video;
				else if (match.Groups["isVideoGame"].Success) movie.Type = MovieBase.MovieType.VideoGame;
				
				movie.IsSuspended = match.Groups["isSuspended"].Success ? true : false;
				if (match.Groups["yearInterval"].Success)
				{
					var foo = match.Groups["yearInterval"].Captures.Count;
					for (int i = 0, l = foo; i < l; i++)
					{
						var match_yi = r_yearInterval.Match(match.Groups["yearInterval"].Captures[i].Value);

						if (match_yi.Success)
						{
							var yi = new YearInterval();
							yi.YearFrom = match_yi.Groups["yearFrom"].Success ? match_yi.Groups["yearFrom"].Value.Equals("????", StringComparison.OrdinalIgnoreCase) ? -1 : int.Parse(match_yi.Groups["yearFrom"].Value) : (int?)null;
							yi.YearTo = match_yi.Groups["yearTo"].Success ? match_yi.Groups["yearTo"].Value.Equals("????", StringComparison.OrdinalIgnoreCase) ? -1 : int.Parse(match_yi.Groups["yearTo"].Value) : (int?)null;
							movie.YearIntervals.Add(yi);
						}
						else
						{
						}
					}

				}
				return movie;
			}
			else return null;
		}
	}

	class ImdbFileHandler_MediaIds : ImdbFileHandler<MediaIdBase>
	{
		protected Regex r_line { get; set; }
		protected Regex r_yearInterval { get; set; }

		public ImdbFileHandler_MediaIds(string data)
			: base(data)
		{
			StartDefinition = null;
			EndDefinition = null;
			ItemDefinition = new Regex(@"(?:^|\n+)\t*(?<line>[^\n]+?)\s*(?=\n+)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

			r_line = new Regex(@"^(?<imdbFullTitle>[^\t]+)\t+(?<globalId>.+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

			GetDataOffset(out _offsetStart, out _offsetEnd);
		}

		protected override MediaIdBase ParseItem(Match item)
		{
			var match = r_line.Match(item.Groups["line"].Value);
			if (match.Success)
			{
				var m = new MediaIdBase();
				m.ImdbFullTitle = match.Groups["imdbFullTitle"].Value;
				m.GlobalId = match.Groups["globalId"].Value;

				return m;
			}
			else return null;
		}
	}

	class ImdbFileHandler_AkaTitles : ImdbFileHandler<AkaTitleBase>
	{
		protected Regex r_title { get; set; }
		protected Regex r_akatitle { get; set; }

		public ImdbFileHandler_AkaTitles(string data)
			: base(data)
		{
			//StartDefinition = new Regex(@"\-{25,}\s*\n+AKA\s+TITLES\s+LIST\s*\n+===============\s*\n+", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
			//EndDefinition = new Regex(@"\n+\-{25,}\s*(?:\n+|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
			StartDefinition = null;
			EndDefinition = null;
			
			ItemDefinition = new Regex(@"(?:^|\n+)\s*(?<line>[^\n]+?)\n(?<akas>(?:[^\n]+?\n)+)?", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

			r_title = new Regex(@"^\s*   " + ImdbFullTitleDefinition + @"   \s*$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
			r_akatitle = new Regex(@"(?:^|\n+)\s*   \(aka\s+" + ImdbFullTitleDefinition + @"\)   (?:\s+\((?<country>.+?)\))?   (?:\s+\((?<description>.+?)\))?   (?=\n+|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
			GetDataOffset(out _offsetStart, out _offsetEnd);
		}

		protected override AkaTitleBase ParseItem(Match item)
		{
			var match = r_title.Match(item.Groups["line"].Value);
			if (match.Success)
			{
				var akatitle = new AkaTitleBase();
				akatitle.ImdbFullTitle = match.Groups["fullTitle"].Value;
				foreach (Match akamatch in r_akatitle.Matches(item.Groups["akas"].Value))
				{
					var aka_td = new AkaTitleDefinition();
					bool isTvSeries = akamatch.Groups["title"].Value.First() == '"' && akamatch.Groups["title"].Value.Last() == '"';
					aka_td.ImdbFullTitle = akamatch.Groups["fullTitle"].Value;
					aka_td.Title = isTvSeries ? akamatch.Groups["title"].Value.Substring(1, akamatch.Groups["title"].Value.Length - 2) : akamatch.Groups["title"].Value;
					aka_td.Country = akamatch.Groups["country"].Success ? akamatch.Groups["country"].Value : null;
					aka_td.Description = akamatch.Groups["description"].Success ? akamatch.Groups["description"].Value : null;
					akatitle.AlsoKnownAs.Add(aka_td);
				}

				return akatitle;
			}
			else return null;
		}
	}

    class ImdbFileHandler_Genres : ImdbFileHandler<GenreBase>
    {
        protected Regex r_title { get; set; }

        public ImdbFileHandler_Genres(string data)
            : base(data)
        {
            //StartDefinition = new Regex(@"\n+\s*8\:\s+THE\s+GENRES\s+LIST\s*\n+==================\s*\n+", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
			StartDefinition = null;
			EndDefinition = null;
            ItemDefinition = new Regex(@"(?:^|\n+)\s*(?<line>[^\n]+?)\s*(?=\n+)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

            r_title = new Regex(@"^\s*   " + ImdbFullTitleDefinition + @"   \s+(?<genre>.+?)   \s*$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

            GetDataOffset(out _offsetStart, out _offsetEnd);
        }

        protected override GenreBase ParseItem(Match item)
        {
            var match = r_title.Match(item.Groups["line"].Value);
            if (match.Success)
            {
                var genre = new GenreBase();
                genre.ImdbFullTitle = match.Groups["fullTitle"].Value;
                genre.Genre = match.Groups["genre"].Value;

                return genre;
            }
            else return null;
        }
    }

    class ImdbFileHandler_Plots : ImdbFileHandler<PlotBase>
    {
        protected Regex r_title { get; set; }
        protected Regex r_plots { get; set; }
        protected Regex r_plot { get; set; }
        protected Regex r_by { get; set; }

        public ImdbFileHandler_Plots(string data)
            : base(data)
        {
            //StartDefinition = new Regex(@"\-{25,}\s*\n+PLOT\s+SUMMARIES\s+LIST\s*\n+===================\s*\n+", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
			StartDefinition = null;
			EndDefinition = null;
            ItemDefinition = new Regex(@"(?:^|\n+)\s*MV\:\s+(?<line>[^\n]+?)\n+(?<plots>(?:(?:[^\n]+?\n)+(?:\n+BY\:[^\n]+?\n+))+)?(?:\-{25,})?", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

            r_title = new Regex(@"^\s*   " + ImdbFullTitleDefinition + @"   \s*$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
            r_plots = new Regex(@"(?:^|\n+)\s*   (?<plot>(?:PL\:[^\n]+?\n)+)   \n+   (?<by>BY\:[^\n]+?\n)   (?=\n+|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
            r_plot = new Regex(@"\s*PL\:\s+", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
            r_by = new Regex(@"\s*BY\:\s+", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
            GetDataOffset(out _offsetStart, out _offsetEnd);
        }

        protected override PlotBase ParseItem(Match item)
        {
            var match = r_title.Match(item.Groups["line"].Value);
            if (match.Success)
            {
                var plot = new PlotBase();
                plot.ImdbFullTitle = match.Groups["fullTitle"].Value;
                foreach (Match plotmatch in r_plots.Matches(item.Groups["plots"].Value))
                {
                    var plot_td = new PlotDefinition();
                    plot_td.Plot = string.Join(" ", plotmatch.Groups["plot"].Value.Trim('\n').Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => r_plot.Replace(x, "")));
                    plot_td.By = r_by.Replace(plotmatch.Groups["by"].Value.Trim('\n'), "");
                    plot.Plots.Add(plot_td);
                }

                return plot;
            }
            else return null;
        }
    }

    class ImdbFileHandler_Ratings : ImdbFileHandler<RatingBase>
    {
        protected Regex r_title { get; set; }

        public ImdbFileHandler_Ratings(string data)
            : base(data)
        {
            //StartDefinition = new Regex(@"\n+MOVIE\s+RATINGS\s+REPORT\s*\n+New\s+Distribution\s+Votes\s+Rank\s+Title\s*\n+", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
            //EndDefinition = new Regex(@"\n+\-{25,}\s*\n+", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
			StartDefinition = null;
			EndDefinition = null;
			ItemDefinition = new Regex(@"(?:^|\n+)\s*(?<line>[^\n]+?)\s*(?=\n+)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

            r_title = new Regex(@"^\s*   (?<distribution>[0-9\.\*]{10})   \s+(?<votes>[0-9]+)   \s+(?<rank>[0-9\.]+)   \s+" + ImdbFullTitleDefinition + @"    \s*$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
            GetDataOffset(out _offsetStart, out _offsetEnd);
        }

        protected override RatingBase ParseItem(Match item)
        {
            var match = r_title.Match(item.Groups["line"].Value);
            if (match.Success)
            {
                var rating = new RatingBase();
                rating.ImdbFullTitle = match.Groups["fullTitle"].Value;
                rating.Distribution = match.Groups["distribution"].Value;
                rating.Votes = int.Parse(match.Groups["votes"].Value);
                rating.Rank = Convert.ToDecimal(match.Groups["rank"].Value, CultureInfo.GetCultureInfo("en-US").NumberFormat);

                return rating;
            }
            else return null;
        }
    }

	abstract class ImdbFileHandler<T> : IImdbFileHandler<T> where T: class, new()
	{
		private string Data { get; set; }
		protected Regex StartDefinition { get; set; }
		protected Regex EndDefinition { get; set; }
		protected Regex ItemDefinition { get; set; }

		protected string ImdbFullTitleDefinition { get; set; }

		protected int _offsetStart;
		protected int OffsetStart
		{
			get { return _offsetStart; }
			set { _offsetStart = value; }
		}

		protected int _offsetEnd;
		protected int OffsetEnd
		{
			get { return _offsetEnd; }
			set { _offsetEnd = value; }
		}

		public ImdbFileHandler(string data)
		{
			Data = data;
			ImdbFullTitleDefinition = @"(?<fullTitle>   (?<fullTitlePartial>   (?<title>.+?)   \s+\((?:(?<year>[0-9]{4,4}|\?\?\?\?)(?:/(?<numeral>[ivx]+))?)\)   )   (?:\s+\{(?:(?=[^{}]{1,1})(?!      \((?:(?:\#[0-9]+\.[0-9]+)|(?:\#[0-9]+)|(?:[0-9]{4,4}(?:-[0-9]{2,2})?(?:-[0-9]{2,2})?))\)\}      )(?:(?<episodeName>.+?)\s*))?   (?:(?<=\s+|\{)\((?:(?:\#(?<season>[0-9]+)\.(?<episode>[0-9]+))|(?:\#(?<episode>[0-9]+))|(?:(?<episodeDate>(?<episodeYear>[0-9]{4,4})(?:-(?<episodeMonth>[0-9]{2,2}))?(?:-(?<episodeDay>[0-9]{2,2}))?)))\))?\})?   (?:\s+\((?<isTvSeries>TV)\))?   (?:\s+\((?<isVideo>V)\))?   (?:\s+\((?<isVideoGame>VG)\))?   (?:\s+(?<isSuspended>\{\{(?:SUSPENDED|SUSPENED)\}\}))?   )";		    
        }

		protected void GetDataOffset(out int start, out int end)
		{
			start = end = -1;

			bool startFound = false, endFound = false;
			var len = 0;
			var buffer_size = (int)Math.Floor((double)(1024 * 1024 * 1) / 4d); //1024 * 1024 * 10 / 4 bytes per char == 10Mb
			var buffer = new char[buffer_size];
			var tmp = new StringBuilder(buffer.Length * 2);

			using (var fs = File.Open(Data, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				using (var sr = new StreamReader(fs, Encoding.GetEncoding(1252)))
				{
					while (!sr.EndOfStream)
					{
						var n = sr.ReadBlock(buffer, 0, buffer.Length);
						if (n > 0)
						{
							if (tmp.Length > buffer.Length)
							{
								tmp.Remove(0, buffer.Length);
								len += buffer.Length;
							}
							tmp.Append(buffer);

							while (!startFound || !endFound)
							{
								if (StartDefinition == null)
								{
									startFound = true;
									start = 0;
								}

								var r = startFound ? EndDefinition : StartDefinition;
								if (startFound && EndDefinition == null)
								{
									//use end of file
									if (sr.EndOfStream)
									{
										endFound = true;
										end = len + tmp.Length - 2;
									}
									break;
								}
								var m = r.Match(tmp.ToString());
								if (m.Success)
								{
									if (!startFound)
									{
										start = len + m.Index + m.Length;
										startFound = true;
										tmp.Remove(0, m.Index + m.Length);
										len += m.Index + m.Length;
									}
									else
									{
										end = len + m.Index - 1;
										endFound = true;
									}
								}
								else break;
							}

							if (startFound && endFound) break;
						}
						else break;
					}

					if(StartDefinition == null && EndDefinition == null && startFound == false && endFound == false)
					{
						//file has no start/end definition and is empty
						start = end = 0;
					}
					else if (startFound == false || endFound == false)
					{
						start = end = -1;
						throw new ApplicationException(string.Format(@"Could not find start/end of data in file '{0}'!", Data));
					}
				}
			}

			tmp = null;
			buffer = null;
		}

		private IEnumerable<string> GetDataChunks(int chunkSize = 1024 * 1024 * 1)
		{
			int chunkSizeInChars = (int)Math.Floor((double)chunkSize / 4d);

			using (var fs = File.Open(Data, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				using (var sr = new StreamReader(fs, Encoding.GetEncoding(1252)))
				{
					var charsRead = 0;
					var buffer = new char[chunkSizeInChars];

					fs.Seek(OffsetStart, SeekOrigin.Begin);
					sr.DiscardBufferedData();
					while (!sr.EndOfStream)
					{
						long len = chunkSizeInChars > OffsetEnd - (OffsetStart + charsRead) + 1 ? OffsetEnd - (OffsetStart + charsRead) + 1 : chunkSizeInChars;
						var n = sr.ReadBlock(buffer, 0, (int)len);
						if (n > 0)
						{
							charsRead += n;
							yield return new string(buffer, 0, n);
						}
						else break;
					}
				}
			}
		}

		private IEnumerable<Match> GetItems()
		{
			string remainingChunk = null;
			foreach (var chunk in GetDataChunks())
			{
				string tmpchunk = remainingChunk != null ? chunk.Insert(0, remainingChunk) : chunk;

				var offset = 0;
				foreach (Match m in ItemDefinition.Matches(tmpchunk))
				{
					yield return m;

					offset = m.Index + m.Length;
				}

				if (tmpchunk.Length > offset) remainingChunk = tmpchunk.Substring(offset);
				else remainingChunk = null;
			}

			if (remainingChunk != null)
			{
				foreach (Match m in ItemDefinition.Matches(string.Concat(remainingChunk, "\n")))
				{
					yield return m;
				}
			}
		}

		public virtual IEnumerable<T> GetParsedItems()
		{
			if (OffsetStart < 0 || OffsetEnd < OffsetStart) throw new ApplicationException(string.Format("Invalid file data offsets (start: {0}, end: {1})!", OffsetStart, OffsetEnd));
			else if (OffsetStart == OffsetEnd) yield break;

            foreach (var item in GetItems())
			{
				var parsedItem = ParseItem(item);
                if (parsedItem != null)
                {
                    yield return parsedItem;
                }
                else Debug.WriteLine(string.Format(@"""{0}"" (failed)", item));
			}
		}

		protected abstract T ParseItem(Match item);
	}
}
