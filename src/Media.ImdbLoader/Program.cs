using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tsebring.Logging;
using MediaHub.ImdbLoader;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Threading;
using MediaHub.ImdbLoader.Entities;
using Raven.Client.Document;
using Raven.Client.Embedded;
using Raven.Client.Indexes;
using System.Reflection;
using Raven.Client;
using Raven.Abstractions.Data;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Raven.Client.Linq;
using Raven.Json.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using MediaHub.Shared;

namespace MediaHub.ImdbLoader
{
	class Program
	{
		private static DocumentStore Store;
		private static ConcurrentDictionary<string, ConcurrentBag<int>> titleMappings = new ConcurrentDictionary<string, ConcurrentBag<int>>(StringComparer.Ordinal);
		private static Encoding encoding = Encoding.GetEncoding(1252);

		private static TElement GetNext<TElement>(IEnumerator<TElement> enumerator)
		{
			enumerator.MoveNext();
			return enumerator.Current;
		}

		static void Main(string[] args)
		{
			bool useLocalFiles = false;
			bool importMedia = true, importGenres = true, importAlsoKnownAs = true, importRatings = true, importPlots = true;
			bool awaitIndexing = true;

			//connection limits, exception handling, ravendb store & indexes
			Initialize();

			//figured indexing after batch import was finished would be faster but the opposite is true
			//turn off indexing
			//ToggleIndexing(false);

			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US"); 

			// Step 1. download imdb flat-file dumps
			if(!useLocalFiles) Console.WriteLine(@"Downloading flat-file dumps from http://ftp.sunet.se/pub/tv+movies/imdb/");
			else Console.WriteLine(@"Loading flat-file dumps from disk");

			var dumpfiles = new List<ImdbDumpFile>();
			if (importMedia) dumpfiles.Add(new ImdbDumpFile(ImdbDumpFileType.Movies, @"http://ftp.sunet.se/pub/tv+movies/imdb/movies.list.gz", useLocalFiles));
			if (importGenres) dumpfiles.Add(new ImdbDumpFile(ImdbDumpFileType.Genres, @"http://ftp.sunet.se/pub/tv+movies/imdb/genres.list.gz", useLocalFiles));
			if (importAlsoKnownAs) dumpfiles.Add(new ImdbDumpFile(ImdbDumpFileType.AlskoKnownAs, @"http://ftp.sunet.se/pub/tv+movies/imdb/aka-titles.list.gz", useLocalFiles));
			if (importRatings) dumpfiles.Add(new ImdbDumpFile(ImdbDumpFileType.Ratings, @"http://ftp.sunet.se/pub/tv+movies/imdb/ratings.list.gz", useLocalFiles));
			if (importPlots) dumpfiles.Add(new ImdbDumpFile(ImdbDumpFileType.Plots, @"http://ftp.sunet.se/pub/tv+movies/imdb/plot.list.gz", useLocalFiles));

			if (!useLocalFiles && dumpfiles.Count > 0)
			{
				var num = 0;
				dumpfiles.ForEach(x =>
				{
					x.Index = ++num;
					x.Filepath = Path.GetTempFileName();
					x.OrderedFilepath = Path.GetTempFileName();

					Console.WriteLine(@"{0}. {1}", x.Index, Path.GetFileName(x.Url));
				});
				Console.WriteLine();

				var cancellationToken = new CancellationToken();
				var progress = new Progress<Dictionary<ImdbDumpFile, DownloadDataProgress>>();
				progress.ProgressChanged += new EventHandler<Dictionary<ImdbDumpFile, DownloadDataProgress>>((sender, e) =>
				{
					var str = string.Join(", ", e.Select(x => string.Format(@"{0}: {1}%", x.Key.Index, x.Value.ProgressPercent)));
					ConsoleReplaceCurrentLine(str);
				});

				var downloadtask = DownloadAllFilesAsync(dumpfiles, cancellationToken, progress);
				downloadtask.Wait();

				Console.Write(nl(2));
			}

			//v2
			var moviefile = dumpfiles.SingleOrDefault(x => x.Type == ImdbDumpFileType.Movies);
			Console.Write(@"Running merge sort on [{0}]...", moviefile.Index);
			MergeSortExternal.Run(new MergeSortExternalDefinition<ByteMovie>
			{
				InPath = moviefile.Filepath,
				OutPath = moviefile.OrderedFilepath,
				ChunkSize = 1024 * 1024 * 25,
				ItemSeparator = "\n",
				StartAt = "MOVIES LIST\n===========\n\n",
				EndAt = "\n--------------------------------------------------------------------------------",
				Enc = Encoding.GetEncoding(1252),
				Comparer = new OrdinalByteArrayComparer<ByteMovie>(i => i.Id),
				ReadItem = (b, d) => new ByteMovie(b, d),
				WriteItem = i => i.GetBytes()
			});
			ConsoleReplaceCurrentLine(string.Format(@"Merge sort complete for [{0}]!", moviefile.Index));
			Console.Write(nl(2));

			var genresfile = dumpfiles.SingleOrDefault(x => x.Type == ImdbDumpFileType.Genres);
			Console.Write(@"Running merge sort on [{0}]...", genresfile.Index);
			MergeSortExternal.Run(new MergeSortExternalDefinition<ByteGenre>
			{
				InPath = genresfile.Filepath,
				OutPath = genresfile.OrderedFilepath,
				ChunkSize = 1024 * 1024 * 25,
				ItemSeparator = "\n",
				StartAt = "8: THE GENRES LIST\n==================\n\n",
				EndAt = null,
				Enc = Encoding.GetEncoding(1252),
				Comparer = new OrdinalByteArrayComparer<ByteGenre>(i => i.Id),
				ReadItem = (b, d) => new ByteGenre(b, d),
				WriteItem = i => i.GetBytes()
			});
			ConsoleReplaceCurrentLine(string.Format(@"Merge sort complete for [{0}]!", genresfile.Index));
			Console.Write(nl(2));

			var alsoknownasfile = dumpfiles.SingleOrDefault(x => x.Type == ImdbDumpFileType.AlskoKnownAs);
			Console.Write(@"Running merge sort on [{0}]...", alsoknownasfile.Index);
			MergeSortExternal.Run(new MergeSortExternalDefinition<ByteAlsoKnownAs>
			{
				InPath = alsoknownasfile.Filepath,
				OutPath = alsoknownasfile.OrderedFilepath,
				ChunkSize = 1024 * 1024 * 25,
				ItemSeparator = "\n\n",
				StartAt = "AKA TITLES LIST\n===============\n\n\n\n",
				EndAt = "\n-----------------------------------------------------------------------------",
				Enc = Encoding.GetEncoding(1252),
				Comparer = new OrdinalByteArrayComparer<ByteAlsoKnownAs>(i => i.Id),
				ReadItem = (b, d) => new ByteAlsoKnownAs(b, d),
				WriteItem = i => i.GetBytes()
			});
			ConsoleReplaceCurrentLine(string.Format(@"Merge sort complete for [{0}]!", alsoknownasfile.Index));
			Console.Write(nl(2));

			var ratingsfile = dumpfiles.SingleOrDefault(x => x.Type == ImdbDumpFileType.Ratings);
			Console.Write(@"Running merge sort on [{0}]...", ratingsfile.Index);
			MergeSortExternal.Run(new MergeSortExternalDefinition<ByteRating>
			{
				InPath = ratingsfile.Filepath,
				OutPath = ratingsfile.OrderedFilepath,
				ChunkSize = 1024 * 1024 * 25,
				ItemSeparator = "\n",
				StartAt = "MOVIE RATINGS REPORT\n\nNew  Distribution  Votes  Rank  Title\n",
				EndAt = "\n\n------------------------------------------------------------------------------\n\nREPORT FORMAT\n=============\n\n",
				Enc = Encoding.GetEncoding(1252),
				Comparer = new OrdinalByteArrayComparer<ByteRating>(i => i.Id),
				ReadItem = (b, d) => new ByteRating(b, d),
				WriteItem = i => i.GetBytes()
			});
			ConsoleReplaceCurrentLine(string.Format(@"Merge sort complete for [{0}]!", ratingsfile.Index));
			Console.Write(nl(2));

			var plotsfile = dumpfiles.SingleOrDefault(x => x.Type == ImdbDumpFileType.Plots);
			Console.Write(@"Running merge sort on [{0}]...", plotsfile.Index);
			MergeSortExternal.Run(new MergeSortExternalDefinition<BytePlot>
			{
				InPath = plotsfile.Filepath,
				OutPath = plotsfile.OrderedFilepath,
				ChunkSize = 1024 * 1024 * 25,
				ItemSeparator = "\n\n-------------------------------------------------------------------------------\n",
				StartAt = "PLOT SUMMARIES LIST\n===================\n\n",
				EndAt = null,
				Enc = Encoding.GetEncoding(1252),
				Comparer = new OrdinalByteArrayComparer<BytePlot>(i => i.Id),
				ReadItem = (b, d) => new BytePlot(b, d),
				WriteItem = i => i.GetBytes()
			});
			ConsoleReplaceCurrentLine(string.Format(@"Merge sort complete for [{0}]!", plotsfile.Index));
			Console.Write(nl(2));

			//Get all MediaIds and save to file
			Console.WriteLine(@"Exporting all MediaIds to file...");
			var mediaIdsPath = Path.GetTempFileName();
			var mediaIdsOrderedPath = Path.GetTempFileName();
			using (var f = File.Open(mediaIdsPath, FileMode.Truncate, FileAccess.Write, FileShare.Read))
			{
				using(var sw = new StreamWriter(f, encoding))
				{
					var n = 0;
					var batchSize = 1024;
					var i = 0;

					while (true)
					{
						using (var session = Store.OpenSession())
						{
							var batch = session.Query<MediaHub.Data.MediaId>()
								.Skip(n)
								.Take(batchSize)
								.ToList();

							if (batch.Count == 0) break;

							foreach (var item in batch)
							{
								sw.Write(string.Format("{0}{1}\t\t\t{2}", (i > 0 ? "\n" : ""), item.ImdbFullTitle, item.GlobalId));
								i++;
							}

							n += batch.Count;
						}
					}
				}
			}

			Console.Write(@"Running merge sort on mediaIds...");
			MergeSortExternal.Run(new MergeSortExternalDefinition<ByteMediaId>
			{
				InPath = mediaIdsPath,
				OutPath = mediaIdsOrderedPath,
				ChunkSize = 1024 * 1024 * 25,
				ItemSeparator = "\n",
				StartAt = null,
				EndAt = null,
				Enc = encoding,
				Comparer = new OrdinalByteArrayComparer<ByteMediaId>(i => i.FullImdbTitle),
				ReadItem = (b, d) => new ByteMediaId(b, d),
				WriteItem = i => i.GetBytes()
			});
			ConsoleReplaceCurrentLine(string.Format(@"Merge sort complete for mediaIds!"));
			Console.Write(nl(2));

			Console.Write(@"Opening and preparing parsing streams...");
			var movies = ImdbFileHandlerFactory.Create<MovieBase>(moviefile.OrderedFilepath).GetParsedItems().GetEnumerator();
			var genres = ImdbFileHandlerFactory.Create<GenreBase>(genresfile.OrderedFilepath).GetParsedItems().GetEnumerator();
			var alsoknownas = ImdbFileHandlerFactory.Create<AkaTitleBase>(alsoknownasfile.OrderedFilepath).GetParsedItems().GetEnumerator();
			var ratings = ImdbFileHandlerFactory.Create<RatingBase>(ratingsfile.OrderedFilepath).GetParsedItems().GetEnumerator();
			var plots = ImdbFileHandlerFactory.Create<PlotBase>(plotsfile.OrderedFilepath).GetParsedItems().GetEnumerator();
			var mediaIds = ImdbFileHandlerFactory.Create<MediaIdBase>(mediaIdsOrderedPath).GetParsedItems().GetEnumerator();
			ConsoleReplaceCurrentLine(string.Format(@"All streams open and ready!"));
			Console.Write(nl(2));

			//point enumerator to first item
			GenreBase genre = GetNext(genres);
			AkaTitleBase aka = GetNext(alsoknownas);
			RatingBase rating = GetNext(ratings);
			PlotBase plot = GetNext(plots);
			MediaIdBase mediaId = GetNext(mediaIds);

			Console.Write("Importing items...");
			var importStarted = DateTime.Now;
			var nParsed = 0;
			var nIds = 0;
			Data.Imdb parent = null;
			List<MediaHub.Data.MediaId> createdIds = new List<MediaHub.Data.MediaId>();

			IDocumentSession rdb = Store.OpenSession();
			try
			{
				var batchSize = 1024;
				var currentOffsetInBatch = 0;	

				while (movies.MoveNext())
				{
					nParsed++;

					var movie = movies.Current;
					var media = new MediaHub.Data.ImdbMedia();
					var imdb = new MediaHub.Data.Imdb()
					{
						ImdbFullTitle = movie.ImdbFullTitle,
						ImdbFullTitlePartial = movie.ImdbFullTitlePartial,
						Title = movie.Title,
						Year = movie.Year,
						Numeral = movie.Numeral,
						YearIntervals = movie.YearIntervals.Select(x => new MediaHub.Data.YearInterval
						{
							YearFrom = x.YearFrom,
							YearTo = x.YearTo
						}).ToList(),
						EpisodeName = movie.EpisodeName,
						EpisodeAiredYear = movie.EpisodeAiredYear,
						EpisodeAiredMonth = movie.EpisodeAiredMonth,
						EpisodeAiredDay = movie.EpisodeAiredDay,
						Season = movie.Season,
						Episode = movie.Episode,
						Type = (MediaHub.Data.MediaType)Enum.Parse(typeof(MediaHub.Data.MediaType), movie.Type.ToString()),
						IsSuspended = movie.IsSuspended
					};
					media.Imdb = imdb;

					if (imdb.Type == Data.MediaType.TvSeries)
					{
						if (!(imdb.Season.HasValue || imdb.Episode.HasValue || !string.IsNullOrEmpty(imdb.EpisodeName) || imdb.EpisodeAiredYear.HasValue || imdb.EpisodeAiredMonth.HasValue || imdb.EpisodeAiredDay.HasValue))
						{
							parent = imdb;
						}
						else if (parent != null)
						{
							if (imdb.ImdbFullTitlePartial.Equals(parent.ImdbFullTitlePartial, StringComparison.Ordinal))
							{
								imdb.ParentMediaId = parent.ParentMediaId;
								imdb.ParentAlsoKnownAs.AddRange(parent.AlsoKnownAs);
								foreach (var g in parent.Genres) imdb.Genres.Add(g);
							}
							else parent = null;
						}
					}

					{
						List<MediaIdBase> skipped;
						var citem = mediaIds
							.SkipWhile(x =>
							{
								var comp = CompareInfo.GetCompareInfo("en-US").Compare(x.ImdbFullTitle, imdb.ImdbFullTitle, CompareOptions.Ordinal);
								return comp < 0;
							}, out skipped)
							.TakeWhile(x => x != null && x.ImdbFullTitle.Equals(imdb.ImdbFullTitle, StringComparison.Ordinal))
							.FirstOrDefault();
						if (citem != null)
						{
							media.Id = string.Format(@"{0}{2}{1}", Store.Conventions.GetTypeTagName(media.GetType()), citem.GlobalId, Store.Conventions.IdentityPartsSeparator);
						}
						else
						{
							var id = new MediaHub.Data.MediaId
							{
								ImdbFullTitle = imdb.ImdbFullTitle,
								GlobalId = Guid.NewGuid().ToString()
							};
							media.Id = string.Format(@"{0}{2}{1}", Store.Conventions.GetTypeTagName(media.GetType()), id.GlobalId, Store.Conventions.IdentityPartsSeparator);

							createdIds.Add(id);
							nIds++;
						}
					}

					{
						List<GenreBase> skipped;
						var citems = genres
							.SkipWhile(x =>
							{
								var comp = CompareInfo.GetCompareInfo("en-US").Compare(x.ImdbFullTitle, imdb.ImdbFullTitle, CompareOptions.Ordinal);
								return comp < 0;
							}, out skipped)
							.TakeWhile(x => x.ImdbFullTitle.Equals(imdb.ImdbFullTitle, StringComparison.Ordinal))
							.Select(x => x.Genre);
						citems.ToList().ForEach(x => imdb.Genres.Add(x));
					}

					{
						List<AkaTitleBase> skipped;
						var citem = alsoknownas
							.SkipWhile(x =>
							{
								var comp = CompareInfo.GetCompareInfo("en-US").Compare(x.ImdbFullTitle, imdb.ImdbFullTitle, CompareOptions.Ordinal);
								return comp < 0;
							}, out skipped)
							.TakeWhile(x => x.ImdbFullTitle.Equals(imdb.ImdbFullTitle, StringComparison.Ordinal))
							.FirstOrDefault();
						if (citem != null)
						{
							imdb.AlsoKnownAs.AddRange(citem.AlsoKnownAs.Select(x => new Data.AlsoKnownAs
							{
								Title = x.Title,
								Country = x.Country,
								Description = x.Description
							}));
						}
					}

					{
						List<RatingBase> skipped;
						var citem = ratings
							.SkipWhile(x =>
							{
								var comp = CompareInfo.GetCompareInfo("en-US").Compare(x.ImdbFullTitle, imdb.ImdbFullTitle, CompareOptions.Ordinal);
								return comp < 0;
							}, out skipped)
							.TakeWhile(x => x.ImdbFullTitle.Equals(imdb.ImdbFullTitle, StringComparison.Ordinal))
							.FirstOrDefault();
						if (citem != null)
						{
							imdb.ImdbRating = new Data.ImdbRating
							{
								Rank = citem.Rank,
								Votes = citem.Votes,
								Distribution1 = citem.GetRatingPercent(1),
								Distribution2 = citem.GetRatingPercent(2),
								Distribution3 = citem.GetRatingPercent(3),
								Distribution4 = citem.GetRatingPercent(4),
								Distribution5 = citem.GetRatingPercent(5),
								Distribution6 = citem.GetRatingPercent(6),
								Distribution7 = citem.GetRatingPercent(7),
								Distribution8 = citem.GetRatingPercent(8),
								Distribution9 = citem.GetRatingPercent(9),
								Distribution10 = citem.GetRatingPercent(10)
							};
						}
					}

					{
						List<PlotBase> skipped;
						var citem = plots
							.SkipWhile(x =>
							{
								var comp = CompareInfo.GetCompareInfo("en-US").Compare(x.ImdbFullTitle, imdb.ImdbFullTitle, CompareOptions.Ordinal);
								return comp < 0;
							}, out skipped)
							.TakeWhile(x => x.ImdbFullTitle.Equals(imdb.ImdbFullTitle, StringComparison.Ordinal))
							.FirstOrDefault();
						if (citem != null)
						{
							imdb.Plots.AddRange(citem.Plots.Select(x => new Data.Plot
							{
								Text = x.Plot,
								By = x.By
							}));
						}
					}

					rdb.Store(media);
					if (++currentOffsetInBatch >= batchSize)
					{
						rdb.SaveChanges();
						rdb.Dispose();
						rdb = Store.OpenSession();

						currentOffsetInBatch = 0;
					}

					if (createdIds.Count >= batchSize)
					{
						using (var rdbid = Store.OpenSession())
						{
							createdIds.ForEach(x => rdbid.Store(x));
							rdbid.SaveChanges();
							createdIds.Clear();
						}
					}

					if (nParsed % 1000 == 0)
					{
						ConsoleReplaceCurrentLine(string.Format(@"{0:N0} items imported and {1:N0} ids created in {2:N1}.", nParsed, nIds, (DateTime.Now-importStarted).ToString(@"hh\:mm\:ss\.f")));
					}
				}
				ConsoleReplaceCurrentLine(string.Format(@"Import completed! {0:N0} items imported and {1:N0} ids created in {2:N1} seconds.", nParsed, nIds, (DateTime.Now - importStarted).ToString(@"hh\:mm\:ss\.f")));
				Console.Write(nl(2));
			}
			finally
			{
				if (rdb != null)
				{
					rdb.SaveChanges();
					rdb.Dispose();
					rdb = null;
				}
			}

			if (!useLocalFiles && dumpfiles.Count > 0)
			{
				dumpfiles.ForEach(x =>
				{
					File.Delete(x.Filepath);
					File.Delete(x.OrderedFilepath);
				});
			}

			//ToggleIndexing(true);

			if (awaitIndexing)
			{
				var fmt = @"Waited {0} for indexing to complete{1}...";
				var fmts = @" (stale: {0})";
				Console.Write(fmt, "", "");
				(Store as IDocumentStore).WaitForIndexing((i, d) =>
				{
					ConsoleReplaceCurrentLine(string.Format(fmt, d.ToString(@"hh\:mm\:ss\.f"), string.Format(fmts, i.Length)));
				});
				ConsoleReplaceCurrentLine(@"Indexing completed!");
				Console.Write(nl(2));
			}

			Console.WriteLine(nl(2) + "Press any key...");
			Console.ReadLine();
		}

		private static string nl(int c)
		{
			var sb = new StringBuilder();
			for (var i = 0; i < c; i++) sb.Append("\r\n");

			return sb.ToString();
		}

		private static void Initialize()
		{
			ServicePointManager.DefaultConnectionLimit = 10;
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
			Store = new DocumentStore { ConnectionStringName = "RavenDB" };
			Store.Conventions.IdentityPartsSeparator = "-";
			Store.Initialize();

			IndexCreation.CreateIndexes(Assembly.GetAssembly(typeof(MediaHub.Data.ImdbMedia)), Store);
		}

		static void ConsoleReplaceCurrentLine(string str)
		{
			var tmp = str + (Console.CursorLeft - str.Length > 0 ? new string(' ', Console.CursorLeft - str.Length) : "");

			Console.Write("\r" + tmp);
		}

		public static void ToggleIndexing(bool on = false)
		{
			try
			{
				using (var client = new WebClient())
				{
					client.UseDefaultCredentials = true;
					var result = client.UploadString(new Uri(new Uri("http://localhost:8080"), string.Format(@"/admin/{0}indexing", on ? "start" : "stop")), "POST", "");
				}
			}
			catch (Exception e)
			{
				throw new Exception("Error encountered while stoppingindexing", e);
			}
		}

		public static RavenJObject GetServerStats()
		{
			var rand = new Random();
			try
			{
				using (var client = new WebClient())
				{
					client.UseDefaultCredentials = true;
					var result = client.DownloadString(new Uri(string.Format(@"http://localhost:8080/stats?noCache={0}", rand.Next())));

					return Raven.Json.Linq.RavenJObject.Parse(result);
				}
			}
			catch (Exception e)
			{
				throw new Exception("Error encountered while getting server stats", e);
			}
		}

		public class IndexedBatch<TBatch> where TBatch : class
		{
			public IndexedBatch(int index, List<TBatch> batch)
			{
				Index = index;
				Batch = batch ?? new List<TBatch>();
			}

			public int Index { get; set; }
			public List<TBatch> Batch { get; set; }
		}

		public enum ImdbDumpFileType { Movies, Genres, Ratings, Plots, AlskoKnownAs }
		public class ImdbDumpFile
		{
			public ImdbDumpFile(ImdbDumpFileType type, string url, bool useLocalFile = false)
			{
				Type = type;
				Url = url;
				
				if (useLocalFile)
				{
					Filepath = @"c:\temp\" + Path.GetFileNameWithoutExtension(url); //get filename without .gz-extension
					OrderedFilepath = Path.ChangeExtension(Filepath, ".orderedlist");
				}
			}

			public ImdbDumpFileType Type { get; set; }
			public int Index { get; set; }
			public string Url { get; set; }
			public string Filepath { get; set; }
			public string OrderedFilepath { get; set; }
		}

		static async Task DownloadAllFilesAsync(IList<ImdbDumpFile> files, CancellationToken cancellationToken, IProgress<Dictionary<ImdbDumpFile, DownloadDataProgress>> progress)
		{
			var plock = new object();
			var p = files.ToDictionary(x => x, x => new DownloadDataProgress());

			var dprogress = new Progress<DownloadDataProgress>();
			dprogress.ProgressChanged += new EventHandler<DownloadDataProgress>((sender, e) =>
			{
				var file = e.UserState as ImdbDumpFile;
				lock (plock)
				{

					if (p.InsertIf((x => x == null || e.Timestamp >= x.Timestamp && e.CurrentBytes > x.CurrentBytes), file, e))
						progress.Report(new Dictionary<ImdbDumpFile, DownloadDataProgress>(p));
				}
			});

			await Task.WhenAll(from f in files select DownloadFileAsync(f, cancellationToken, dprogress));

			foreach (var _p in p)
			{
				_p.Value.CurrentBytes = _p.Value.TotalBytes;
				_p.Value.Timestamp = DateTime.UtcNow.AddYears(1);
			}
			(dprogress as IProgress<DownloadDataProgress>).Report(new DownloadDataProgress(p.First().Value.TotalBytes, p.First().Value.TotalBytes) { UserState = p.First().Value.UserState, Timestamp = DateTime.UtcNow.AddYears(2) });
		}

		static async Task DownloadFileAsync(ImdbDumpFile file, CancellationToken cancellationToken, IProgress<DownloadDataProgress> progress)
		{
			var archivepath = Path.GetTempFileName();
			using (var client = new WebClient())
			{
				var dprogress = new Progress<DownloadDataProgress>();
				dprogress.ProgressChanged += new EventHandler<DownloadDataProgress>((sender, e) =>
				{
					e.UserState = file;
					progress.Report(e);
				});

				var dl = client.DownloadFileTaskAsync(file.Url, archivepath, cancellationToken, dprogress);
				await dl;
				if (dl.IsCompleted)
				{
					using (var fpo = File.Open(file.Filepath, FileMode.Create, FileAccess.Write, FileShare.Read))
					{
						using (var fpi = File.Open(archivepath, FileMode.Open, FileAccess.Read, FileShare.Read))
						{
							using (GZipStream decompress = new GZipStream(fpi, CompressionMode.Decompress))
							{
								decompress.CopyTo(fpo);
							}
						}
					}

					File.Delete(archivepath);
				}
			}
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject is Exception) ExceptionLogging.LogUnhandledException(e.ExceptionObject as Exception, true);
		}
	}
}
