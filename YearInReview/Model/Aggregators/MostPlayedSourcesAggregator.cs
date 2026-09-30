using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using YearInReview.Extensions.GameActivity;
using YearInReview.Model.Aggregators.Data;

namespace YearInReview.Model.Aggregators
{
	public class MostPlayedSourcesAggregator : IMostPlayedSourcesAggregator
	{
		private readonly IPlayniteAPI _playniteApi;

		public MostPlayedSourcesAggregator(IPlayniteAPI playniteApi)
		{
			_playniteApi = playniteApi;
		}

		public IReadOnlyCollection<SourceWithTime> GetMostPlayedSources(IReadOnlyCollection<Activity> activities)
		{
			return activities.SelectMany(x => x.Items)
				.GroupBy(GetSourceGroupKey)
				.Select(x => new SourceWithTime
				{
					Source = ResolveSource(x),
					TimePlayed = x.Sum(s => s.ElapsedSeconds),
				})
				.Where(x => x.Source != null)
				.OrderByDescending(x => x.TimePlayed)
				.ToList();
		}

		private static object GetSourceGroupKey(Session session)
		{
			if (session.SourceId != Guid.Empty)
			{
				return session.SourceId;
			}

			// Sessions without a source id and without a source name are treated as
			// played directly through Playnite, keeping the pre-existing behavior.
			return string.IsNullOrEmpty(session.SourceName)
				? (object)Guid.Empty
				: $"name:{session.SourceName}";
		}

		private GameSource ResolveSource(IGrouping<object, Session> sessions)
		{
			var sourceId = sessions.Key as Guid?;
			if (sourceId == Guid.Empty)
			{
				return new GameSource
				{
					Id = Guid.Empty,
					Name = "Playnite",
				};
			}

			if (sourceId.HasValue)
			{
				return _playniteApi.Database.Sources.FirstOrDefault(s => s.Id == sourceId.Value);
			}

			var sourceName = GetFirstSourceName(sessions);
			var databaseSource = sourceName == null
				? null
				: _playniteApi.Database.Sources.FirstOrDefault(s =>
					string.Equals(s.Name, sourceName, StringComparison.OrdinalIgnoreCase));
			return databaseSource ?? new GameSource
			{
				Id = Guid.Empty,
				Name = sourceName
			};
		}

		private static string GetFirstSourceName(IEnumerable<Session> sessions)
		{
			return sessions.Select(x => x.SourceName).FirstOrDefault(x => !string.IsNullOrEmpty(x));
		}
	}
}
