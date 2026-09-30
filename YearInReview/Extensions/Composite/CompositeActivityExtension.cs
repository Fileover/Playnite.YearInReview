using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YearInReview.Extensions.GameActivity;

namespace YearInReview.Extensions.Composite
{
	/// <summary>
	/// Merges activities from GameActivity and PlaytimeInsights.
	/// PlaytimeInsights sessions take precedence over GameActivity sessions for the same game and day,
	/// so tracked days are not counted twice. Games without PlaytimeInsights data fall back to GameActivity.
	/// </summary>
	public class CompositeActivityExtension : IGameActivityExtension
	{
		private readonly IGameActivityExtension _gameActivityExtension;
		private readonly IGameActivityExtension _playtimeInsightsExtension;

		public CompositeActivityExtension(
			IGameActivityExtension gameActivityExtension,
			IGameActivityExtension playtimeInsightsExtension)
		{
			_gameActivityExtension = gameActivityExtension;
			_playtimeInsightsExtension = playtimeInsightsExtension;
		}

		public async Task<IReadOnlyCollection<Activity>> GetActivityForGames(IEnumerable<Game> games)
		{
			var gamesList = games.ToList();
			var gameActivityActivities = await _gameActivityExtension.GetActivityForGames(gamesList);
			var playtimeInsightsActivities = await _playtimeInsightsExtension.GetActivityForGames(gamesList);

			if (playtimeInsightsActivities.Count == 0)
			{
				return gameActivityActivities;
			}

			if (gameActivityActivities.Count == 0)
			{
				return playtimeInsightsActivities;
			}

			var mergedActivities = new List<Activity>();
			var mergedGameIds = new HashSet<Guid>();

			var insightsByGameId = playtimeInsightsActivities.ToDictionary(x => x.Id);
			foreach (var gameActivityGroup in gameActivityActivities.GroupBy(x => x.Id))
			{
				var gameId = gameActivityGroup.Key;
				var allGameActivitySessions = gameActivityGroup
					.SelectMany(x => x.Items)
					.ToList();
				var gameActivityName = gameActivityGroup
					.Select(x => x.Name)
					.FirstOrDefault(x => !string.IsNullOrEmpty(x));

				if (!insightsByGameId.TryGetValue(gameId, out var insightsActivity))
				{
					mergedActivities.Add(new Activity
					{
						Id = gameId,
						Name = gameActivityName,
						Items = allGameActivitySessions
					});
					mergedGameIds.Add(gameId);
					continue;
				}

				var daysWithInsights = new HashSet<DateTime>(
					insightsActivity.Items.Select(x => x.DateSession.Date));
				var gameActivitySessions = allGameActivitySessions
					.Where(x => !daysWithInsights.Contains(x.DateSession.Date))
					.ToList();

				var items = new List<Session>();
				items.AddRange(gameActivitySessions);
				items.AddRange(insightsActivity.Items);

				mergedActivities.Add(new Activity
				{
					Id = gameId,
					Name = FirstNonEmpty(gameActivityName, insightsActivity.Name),
					Items = items
				});
				mergedGameIds.Add(gameId);
			}

			mergedActivities.AddRange(playtimeInsightsActivities
				.Where(x => !mergedGameIds.Contains(x.Id)));

			return mergedActivities;
		}

		private static string FirstNonEmpty(string first, string second)
		{
			return string.IsNullOrEmpty(first) ? second : first;
		}
	}
}
