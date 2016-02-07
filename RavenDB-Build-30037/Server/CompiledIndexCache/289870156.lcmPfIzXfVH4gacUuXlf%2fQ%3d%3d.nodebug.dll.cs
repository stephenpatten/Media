using Raven.Abstractions;
using Raven.Database.Linq;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System;
using Raven.Database.Linq.PrivateExtensions;
using Lucene.Net.Documents;
using System.Globalization;
using System.Text.RegularExpressions;
using Raven.Database.Indexing;

public class Index_ImdbMedias_Search : Raven.Database.Linq.AbstractViewGenerator
{
	public Index_ImdbMedias_Search()
	{
		this.ViewText = @"from m in docs.ImdbMedias
select new {
	Title = m.Imdb.Title,
	FTitle = m.Imdb.Title,
	FAlsoKnownAs = Enumerable.ToArray(DynamicEnumerable.Concat(m.Imdb.AlsoKnownAs.Select(x => x.Title), m.Imdb.ParentAlsoKnownAs.Select(x0 => x0.Title))),
	ETitle = m.Imdb.Title,
	EAlsoKnownAs = Enumerable.ToArray(DynamicEnumerable.Concat(m.Imdb.AlsoKnownAs.Select(x1 => x1.Title), m.Imdb.ParentAlsoKnownAs.Select(x2 => x2.Title))),
	EpisodeName = m.Imdb.EpisodeName,
	Year = m.Imdb.Year,
	Type = m.Imdb.Type,
	Season = m.Imdb.Season,
	Episode = m.Imdb.Episode
}";
		this.ForEntityNames.Add("ImdbMedias");
		this.AddMapDefinition(docs => 
			from m in ((IEnumerable<dynamic>)docs)
			where string.Equals(m["@metadata"]["Raven-Entity-Name"], "ImdbMedias", System.StringComparison.InvariantCultureIgnoreCase)
			select new {
				Title = m.Imdb.Title,
				FTitle = m.Imdb.Title,
				FAlsoKnownAs = Enumerable.ToArray(DynamicEnumerable.Concat(m.Imdb.AlsoKnownAs.Select((Func<dynamic, dynamic>)(x => x.Title)), m.Imdb.ParentAlsoKnownAs.Select((Func<dynamic, dynamic>)(x0 => x0.Title)))),
				ETitle = m.Imdb.Title,
				EAlsoKnownAs = Enumerable.ToArray(DynamicEnumerable.Concat(m.Imdb.AlsoKnownAs.Select((Func<dynamic, dynamic>)(x1 => x1.Title)), m.Imdb.ParentAlsoKnownAs.Select((Func<dynamic, dynamic>)(x2 => x2.Title)))),
				EpisodeName = m.Imdb.EpisodeName,
				Year = m.Imdb.Year,
				Type = m.Imdb.Type,
				Season = m.Imdb.Season,
				Episode = m.Imdb.Episode,
				__document_id = m.__document_id
			});
		this.AddField("Title");
		this.AddField("FTitle");
		this.AddField("FAlsoKnownAs");
		this.AddField("ETitle");
		this.AddField("EAlsoKnownAs");
		this.AddField("EpisodeName");
		this.AddField("Year");
		this.AddField("Type");
		this.AddField("Season");
		this.AddField("Episode");
		this.AddField("__document_id");
		this.AddQueryParameterForMap("Imdb.Title");
		this.AddQueryParameterForMap("Imdb.EpisodeName");
		this.AddQueryParameterForMap("Imdb.Year");
		this.AddQueryParameterForMap("Imdb.Type");
		this.AddQueryParameterForMap("Imdb.Season");
		this.AddQueryParameterForMap("Imdb.Episode");
		this.AddQueryParameterForMap("__document_id");
		this.AddQueryParameterForReduce("Imdb.Title");
		this.AddQueryParameterForReduce("Imdb.EpisodeName");
		this.AddQueryParameterForReduce("Imdb.Year");
		this.AddQueryParameterForReduce("Imdb.Type");
		this.AddQueryParameterForReduce("Imdb.Season");
		this.AddQueryParameterForReduce("Imdb.Episode");
		this.AddQueryParameterForReduce("__document_id");
	}
}
