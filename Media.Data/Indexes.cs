using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace MediaHub.Data
{
	public class ImdbMedias_Search : AbstractIndexCreationTask<ImdbMedia, ImdbMedias_Search.Result>
	{
		public class Result
		{
			public string Title { get; set; }
			public string FTitle;
			public string[] FAlsoKnownAs;
			public string ETitle;
			public string[] EAlsoKnownAs;
			public string EpisodeName;
			public int Year { get; set; }
			public MediaType Type { get; set; }
			public int? Season { get; set; }
			public int? Episode { get; set; }
		}

		public ImdbMedias_Search()
		{
			Map = medias => from m in medias
							select new
							{
								Title = m.Imdb.Title,
								FTitle = m.Imdb.Title,
								FAlsoKnownAs = m.Imdb.AlsoKnownAs.Select(x => x.Title).Concat(m.Imdb.ParentAlsoKnownAs.Select(x => x.Title)).ToArray(),
								ETitle = m.Imdb.Title,
								EAlsoKnownAs = m.Imdb.AlsoKnownAs.Select(x => x.Title).Concat(m.Imdb.ParentAlsoKnownAs.Select(x => x.Title)).ToArray(),
								EpisodeName = m.Imdb.EpisodeName,
								Year = m.Imdb.Year,
								Type = m.Imdb.Type,
								Season = m.Imdb.Season,
								Episode = m.Imdb.Episode
							};

			Index(x => x.Title, FieldIndexing.NotAnalyzed);
			Index(x => x.FTitle, FieldIndexing.Analyzed);
			Index(x => x.FAlsoKnownAs, FieldIndexing.Analyzed);
			Index(x => x.ETitle, FieldIndexing.Analyzed);
			Index(x => x.EAlsoKnownAs, FieldIndexing.Analyzed);
			Index(x => x.EpisodeName, FieldIndexing.Analyzed);
			Index(x => x.Year, FieldIndexing.Default);
			Index(x => x.Type, FieldIndexing.Default);
			Index(x => x.Season, FieldIndexing.Default);
			Index(x => x.Episode, FieldIndexing.Default);

			Sort(x => x.Title, SortOptions.String);
			Sort(x => x.Year, SortOptions.Int);
			Sort(x => x.Season, SortOptions.Int);
			Sort(x => x.Episode, SortOptions.Int);

			Analyze(x => x.FTitle, typeof(CharJoinAbbrLowerCaseReplacementAnalyzer).AssemblyQualifiedName);
			Analyze(x => x.FAlsoKnownAs, typeof(CharJoinAbbrLowerCaseReplacementAnalyzer).AssemblyQualifiedName);
			Analyze(x => x.ETitle, typeof(CharJoinAbbrLowerCaseExactAnalyzer).AssemblyQualifiedName);
			Analyze(x => x.EAlsoKnownAs, typeof(CharJoinAbbrLowerCaseExactAnalyzer).AssemblyQualifiedName);
			Analyze(x => x.EpisodeName, typeof(CharJoinAbbrLowerCaseReplacementAnalyzer).AssemblyQualifiedName);
		}
	}
}
