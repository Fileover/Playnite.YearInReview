using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YearInReview.Extensions.GameActivity;

namespace YearInReview.Extensions.PlaytimeInsights
{
	public class PlaytimeInsightsExtension : IPlaytimeInsightsExtension
	{
		public static Guid ExtensionId = Guid.Parse("7094cd6b-d3a4-41d0-b7c3-f0cc535a9efd");

		private const string SessionsFileName = "sessions.json";

		private readonly ILogger _logger = LogManager.GetLogger(nameof(PlaytimeInsightsExtension));
		private readonly string _sessionsPath;

		public PlaytimeInsightsExtension(string extensionsDataPath)
		{
			_sessionsPath = Path.Combine(extensionsDataPath, ExtensionId.ToString(), SessionsFileName);
		}

		public bool IsDataAvailable()
		{
			return File.Exists(_sessionsPath);
		}

		public async Task<IReadOnlyCollection<Activity>> GetActivityForGames(IEnumerable<Game> games)
		{
			var gamesById = games.ToDictionary(x => x.Id);

			try
			{
				if (!IsDataAvailable())
				{
					_logger.Warn("PlaytimeInsights sessions file does not exist!");
					return new List<Activity>();
				}

				var store = await ReadStore();
				if (store?.Sessions == null || store.Sessions.Count == 0)
				{
					_logger.Warn("PlaytimeInsights sessions file contains no sessions!");
					return new List<Activity>();
				}

				var activities = new Dictionary<Guid, Activity>();
				foreach (var session in store.Sessions)
				{
					if (session == null || session.IsDeleted)
					{
						continue;
					}

					if (!gamesById.ContainsKey(session.GameId))
					{
						continue;
					}

					var gameId = session.GameId;
					if (!activities.TryGetValue(gameId, out var activity))
					{
						activity = new Activity
						{
							Id = gameId,
							Name = session.GameName
						};
						activities.Add(gameId, activity);
					}

					activity.Items.Add(ToSession(session));
				}

				_logger.Info($"{activities.Count} games with PlaytimeInsights activity found");
				return activities.Values.ToList();
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Failure reading PlaytimeInsights sessions file");
				return new List<Activity>();
			}
		}

		private async Task<PlaytimeInsightsStore> ReadStore()
		{
			using (var reader = new StreamReader(_sessionsPath))
			{
				var json = await reader.ReadToEndAsync();
				return JsonConvert.DeserializeObject<PlaytimeInsightsStore>(json);
			}
		}

		private static Session ToSession(PlaytimeInsightsSession insightsSession)
		{
			return new Session
			{
				DateSession = insightsSession.StartedAtUtc.ToLocalTime(),
				ElapsedSeconds = ToIntSeconds(insightsSession.ElapsedSeconds),
				SourceId = Guid.Empty,
				SourceName = insightsSession.GameSourceName,
				PlatformIDs = new List<Guid>(),
				IdConfiguration = 0
			};
		}

		private static int ToIntSeconds(ulong elapsedSeconds)
		{
			return elapsedSeconds > int.MaxValue ? int.MaxValue : (int)elapsedSeconds;
		}
	}
}
