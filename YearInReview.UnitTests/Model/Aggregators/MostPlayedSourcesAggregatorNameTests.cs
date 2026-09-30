using AutoFixture.Xunit2;
using FakeItEasy;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using TestTools.Shared;
using Xunit;
using YearInReview.Extensions.GameActivity;
using YearInReview.Model.Aggregators;

namespace YearInReview.UnitTests.Model.Aggregators
{
	public class MostPlayedSourcesAggregatorNameTests
	{
		[Fact]
		public void GetMostPlayedSources_ResolvesExternalSourceByName_WhenSourceIdIsEmpty()
		{
			// Arrange
			var playniteApi = A.Fake<IPlayniteAPI>();
			var gameDatabaseApi = A.Fake<IGameDatabaseAPI>();
			var sources = new List<GameSource>
			{
				new GameSource { Id = Guid.NewGuid(), Name = "Steam" }
			};
			A.CallTo(() => playniteApi.Database).Returns(gameDatabaseApi);
			A.CallTo(() => gameDatabaseApi.Sources).Returns(new TestableItemCollection<GameSource>(sources));

			var activities = new List<Activity>
			{
				new Activity
				{
					Id = Guid.NewGuid(),
					Items = new List<Session>
					{
						new Session { ElapsedSeconds = 600, SourceId = Guid.Empty, SourceName = "Steam" },
						new Session { ElapsedSeconds = 1200, SourceId = Guid.Empty, SourceName = "Steam" },
						new Session { ElapsedSeconds = 60, SourceId = Guid.Empty, SourceName = "Epic" }
					}
				}
			};
			var sut = new MostPlayedSourcesAggregator(playniteApi);

			// Act
			var result = sut.GetMostPlayedSources(activities);

			// Assert
			Assert.Equal(2, result.Count);
			Assert.Equal("Steam", result.First().Source.Name);
			Assert.Equal(1800, result.First().TimePlayed);
			Assert.Equal("Epic", result.Last().Source.Name);
			Assert.Equal(60, result.Last().TimePlayed);
		}

		[Fact]
		public void GetMostPlayedSources_UsesDatabaseSource_WhenNameMatchesExistingSource()
		{
			// Arrange
			var playniteApi = A.Fake<IPlayniteAPI>();
			var gameDatabaseApi = A.Fake<IGameDatabaseAPI>();
			var steamSource = new GameSource { Id = Guid.NewGuid(), Name = "Steam" };
			var sources = new List<GameSource> { steamSource };
			A.CallTo(() => playniteApi.Database).Returns(gameDatabaseApi);
			A.CallTo(() => gameDatabaseApi.Sources).Returns(new TestableItemCollection<GameSource>(sources));

			var activities = new List<Activity>
			{
				new Activity
				{
					Id = Guid.NewGuid(),
					Items = new List<Session>
					{
						new Session { ElapsedSeconds = 500, SourceId = Guid.Empty, SourceName = "steam" }
					}
				}
			};
			var sut = new MostPlayedSourcesAggregator(playniteApi);

			// Act
			var result = sut.GetMostPlayedSources(activities);

			// Assert
			var steam = Assert.Single(result);
			Assert.Equal(steamSource.Id, steam.Source.Id);
			Assert.Equal(500, steam.TimePlayed);
		}

		[Fact]
		public void GetMostPlayedSources_TreatsSessionsWithEmptySourceName_AsPlaynite()
		{
			// Arrange
			var playniteApi = A.Fake<IPlayniteAPI>();
			var gameDatabaseApi = A.Fake<IGameDatabaseAPI>();
			A.CallTo(() => playniteApi.Database).Returns(gameDatabaseApi);
			A.CallTo(() => gameDatabaseApi.Sources).Returns(new TestableItemCollection<GameSource>(new List<GameSource>()));

			var activities = new List<Activity>
			{
				new Activity
				{
					Id = Guid.NewGuid(),
					Items = new List<Session>
					{
						new Session { ElapsedSeconds = 500, SourceId = Guid.Empty, SourceName = null }
					}
				}
			};
			var sut = new MostPlayedSourcesAggregator(playniteApi);

			// Act
			var result = sut.GetMostPlayedSources(activities);

			// Assert
			var playnite = Assert.Single(result);
			Assert.Equal("Playnite", playnite.Source.Name);
		}

		[Theory]
		[AutoFakeItEasyData]
		public void GetMostPlayedSources_StillResolvesDatabaseSources_BySourceId(
			[Frozen] IGameDatabaseAPI gameDatabaseApiFake,
			[Frozen] IPlayniteAPI playniteApiFake,
			Guid sourceId,
			List<Activity> activities,
			MostPlayedSourcesAggregator sut)
		{
			// Arrange
			var sources = activities.SelectMany(x => x.Items).Select(x => new GameSource { Id = x.SourceId }).ToList();
			sources.ForEach(x => x.Id = sourceId);
			activities.ForEach(x => x.Items.ForEach(s => s.SourceId = sourceId));
			A.CallTo(() => playniteApiFake.Database).Returns(gameDatabaseApiFake);
			A.CallTo(() => gameDatabaseApiFake.Sources).Returns(new TestableItemCollection<GameSource>(sources));

			// Act
			var result = sut.GetMostPlayedSources(activities);

			// Assert
			Assert.All(result, x => Assert.Equal(sourceId, x.Source.Id));
		}
	}
}
