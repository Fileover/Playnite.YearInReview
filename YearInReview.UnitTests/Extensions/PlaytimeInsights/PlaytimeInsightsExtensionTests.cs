using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using YearInReview.Extensions.PlaytimeInsights;

namespace YearInReview.UnitTests.Extensions.PlaytimeInsights
{
	public class PlaytimeInsightsExtensionTests : IDisposable
	{
		private readonly string _extensionsDataPath;

		public PlaytimeInsightsExtensionTests()
		{
			_extensionsDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_extensionsDataPath);
		}

		public void Dispose()
		{
			if (Directory.Exists(_extensionsDataPath))
			{
				Directory.Delete(_extensionsDataPath, true);
			}
		}

		[Fact]
		public async Task GetActivityForGames_ReturnsNoActivities_WhenSessionsFileDoesNotExist()
		{
			// Arrange
			var sut = CreateExtension();

			// Act
			var result = await sut.GetActivityForGames(new List<Playnite.SDK.Models.Game>());

			// Assert
			Assert.Empty(result);
		}

		[Fact]
		public async Task GetActivityForGames_MapsSessions_WhenExampleStoreProvided()
		{
			// Arrange
			WriteSessionsFile(GetSampleSessionsJson());
			var sut = CreateExtension();
			var games = new List<Playnite.SDK.Models.Game>
			{
				new Playnite.SDK.Models.Game { Id = Guid.Parse("ec0aba5d-154f-41ae-a390-4375096da45f") },
				new Playnite.SDK.Models.Game { Id = Guid.Parse("1838583b-3ac1-4fe4-8ed3-0d4a6a90fa6d") },
				new Playnite.SDK.Models.Game { Id = Guid.Parse("46d53684-59fa-4bb4-9ee6-b7b6d35e2f64") },
				new Playnite.SDK.Models.Game { Id = Guid.Parse("d076b325-d1c0-4e88-abef-2e1f8961e829") },
				new Playnite.SDK.Models.Game { Id = Guid.Parse("9140a7b5-0420-44e4-b9bd-5402e1032597") },
				new Playnite.SDK.Models.Game { Id = Guid.Parse("b1b544d7-4780-4234-a6e4-2beb5574ac33") },
				new Playnite.SDK.Models.Game { Id = Guid.Parse("0835026c-0d0b-467d-a6d9-f69f450a96db") }
			};

			// Act
			var result = await sut.GetActivityForGames(games);

			// Assert
			Assert.Equal(7, result.Count);
			var sevenDeadlySins = result.First(x => x.Name == "The Seven Deadly Sins: Origin");
			Assert.Equal(12, sevenDeadlySins.Items.Count);
			Assert.All(sevenDeadlySins.Items, x => Assert.Equal("Steam", x.SourceName));
			var firstSession = sevenDeadlySins.Items.OrderBy(x => x.DateSession).First();
			Assert.Equal(8031, firstSession.ElapsedSeconds);
		}

		[Fact]
		public async Task GetActivityForGames_SkipsDeletedSessionsAndUnknownGames()
		{
			// Arrange
			var knownGameId = Guid.NewGuid();
			var deletedGameId = Guid.NewGuid();
			var store = new PlaytimeInsightsStore
			{
				Sessions = new List<PlaytimeInsightsSession>
				{
					new PlaytimeInsightsSession
					{
						Id = Guid.NewGuid(),
						GameId = deletedGameId,
						GameName = "Deleted Game",
						StartedAtUtc = new DateTime(2025, 5, 1, 10, 0, 0, DateTimeKind.Utc),
						EndedAtUtc = new DateTime(2025, 5, 1, 11, 0, 0, DateTimeKind.Utc),
						ElapsedSeconds = 3600,
						IsDeleted = true
					},
					new PlaytimeInsightsSession
					{
						Id = Guid.NewGuid(),
						GameId = Guid.NewGuid(),
						GameName = "Unknown Game",
						StartedAtUtc = new DateTime(2025, 5, 1, 10, 0, 0, DateTimeKind.Utc),
						EndedAtUtc = new DateTime(2025, 5, 1, 11, 0, 0, DateTimeKind.Utc),
						ElapsedSeconds = 3600
					},
					new PlaytimeInsightsSession
					{
						Id = Guid.NewGuid(),
						GameId = knownGameId,
						GameName = "Known Game",
						StartedAtUtc = new DateTime(2025, 5, 1, 10, 0, 0, DateTimeKind.Utc),
						EndedAtUtc = new DateTime(2025, 5, 1, 11, 0, 0, DateTimeKind.Utc),
						ElapsedSeconds = 3600
					}
				}
			};
			WriteSessionsFile(JsonConvert.SerializeObject(store));
			var sut = CreateExtension();
			var games = new List<Playnite.SDK.Models.Game> { new Playnite.SDK.Models.Game { Id = knownGameId } };

			// Act
			var result = await sut.GetActivityForGames(games);

			// Assert
			var activity = Assert.Single(result);
			Assert.Equal(knownGameId, activity.Id);
			Assert.Equal("Known Game", activity.Name);
		}

		[Fact]
		public async Task GetActivityForGames_ClampsHugeSessions_ToIntMaxValue()
		{
			// Arrange
			var gameId = Guid.NewGuid();
			var store = new PlaytimeInsightsStore
			{
				Sessions = new List<PlaytimeInsightsSession>
				{
					new PlaytimeInsightsSession
					{
						Id = Guid.NewGuid(),
						GameId = gameId,
						GameName = "Long Game",
						StartedAtUtc = new DateTime(2025, 5, 1, 10, 0, 0, DateTimeKind.Utc),
						EndedAtUtc = new DateTime(2025, 5, 1, 11, 0, 0, DateTimeKind.Utc),
						ElapsedSeconds = ulong.MaxValue
					}
				}
			};
			WriteSessionsFile(JsonConvert.SerializeObject(store));
			var sut = CreateExtension();
			var games = new List<Playnite.SDK.Models.Game> { new Playnite.SDK.Models.Game { Id = gameId } };

			// Act
			var result = await sut.GetActivityForGames(games);

			// Assert
			Assert.Equal(int.MaxValue, result.Single().Items.Single().ElapsedSeconds);
		}

		private PlaytimeInsightsExtension CreateExtension()
		{
			return new PlaytimeInsightsExtension(_extensionsDataPath);
		}

		private static string GetSampleSessionsJson()
		{
			var assembly = Assembly.GetExecutingAssembly();
			using (var stream = assembly.GetManifestResourceStream("PlaytimeInsightsSampleSessions.json"))
			{
				Assert.NotNull(stream);
				using (var reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			}
		}

		private void WriteSessionsFile(string contents)
		{
			var pluginDataDirectory = Path.Combine(
				_extensionsDataPath,
				PlaytimeInsightsExtension.ExtensionId.ToString());
			Directory.CreateDirectory(pluginDataDirectory);
			File.WriteAllText(
				Path.Combine(pluginDataDirectory, "sessions.json"),
				contents,
				new UTF8Encoding(false));
		}
	}
}
