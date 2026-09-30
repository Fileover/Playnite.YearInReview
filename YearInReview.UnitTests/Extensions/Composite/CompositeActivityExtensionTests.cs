using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Playnite.SDK.Models;
using Xunit;
using YearInReview.Extensions.Composite;
using YearInReview.Extensions.GameActivity;

namespace YearInReview.UnitTests.Extensions.Composite
{
	public class CompositeActivityExtensionTests
	{
		[Fact]
		public async Task GetActivityForGames_ReturnsGameActivityOnly_WhenNoPlaytimeInsightsData()
		{
			// Arrange
			var gameActivity = A.Fake<IGameActivityExtension>();
			var playtimeInsights = A.Fake<IGameActivityExtension>();
			var activities = new List<Activity> { CreateActivity(Guid.NewGuid(), new DateTime(2025, 3, 1), 100) };
			A.CallTo(() => gameActivity.GetActivityForGames(A<IEnumerable<Game>>._)).Returns(activities);
			A.CallTo(() => playtimeInsights.GetActivityForGames(A<IEnumerable<Game>>._)).Returns(new List<Activity>());
			var sut = new CompositeActivityExtension(gameActivity, playtimeInsights);

			// Act
			var result = await sut.GetActivityForGames(new List<Game>());

			// Assert
			Assert.Equal(activities, result);
		}

		[Fact]
		public async Task GetActivityForGames_ReturnsPlaytimeInsightsOnly_WhenNoGameActivityData()
		{
			// Arrange
			var gameActivity = A.Fake<IGameActivityExtension>();
			var playtimeInsights = A.Fake<IGameActivityExtension>();
			var activities = new List<Activity> { CreateActivity(Guid.NewGuid(), new DateTime(2025, 3, 1), 100) };
			A.CallTo(() => gameActivity.GetActivityForGames(A<IEnumerable<Game>>._)).Returns(new List<Activity>());
			A.CallTo(() => playtimeInsights.GetActivityForGames(A<IEnumerable<Game>>._)).Returns(activities);
			var sut = new CompositeActivityExtension(gameActivity, playtimeInsights);

			// Act
			var result = await sut.GetActivityForGames(new List<Game>());

			// Assert
			Assert.Equal(activities, result);
		}

		[Fact]
		public async Task GetActivityForGames_PrefersPlaytimeInsightsSessions_OnSameDay()
		{
			// Arrange
			var gameId = Guid.NewGuid();
			var day = new DateTime(2025, 3, 1, 10, 0, 0);
			var gameActivity = A.Fake<IGameActivityExtension>();
			var playtimeInsights = A.Fake<IGameActivityExtension>();
			A.CallTo(() => gameActivity.GetActivityForGames(A<IEnumerable<Game>>._)).Returns(new List<Activity>
			{
				CreateActivity(gameId, day, 100),
				CreateActivity(gameId, day.AddHours(2), 200)
			});
			A.CallTo(() => playtimeInsights.GetActivityForGames(A<IEnumerable<Game>>._)).Returns(new List<Activity>
			{
				CreateActivity(gameId, day.AddHours(1), 1000)
			});
			var sut = new CompositeActivityExtension(gameActivity, playtimeInsights);

			// Act
			var result = await sut.GetActivityForGames(new List<Game>());

			// Assert
			var activity = Assert.Single(result);
			Assert.Equal(2, activity.Items.Count);
			Assert.Contains(activity.Items, x => x.DateSession == day.AddHours(1) && x.ElapsedSeconds == 1000);
			Assert.Contains(activity.Items, x => x.DateSession == day.AddHours(2) && x.ElapsedSeconds == 200);
			Assert.DoesNotContain(activity.Items, x => x.DateSession == day && x.ElapsedSeconds == 100);
		}

		[Fact]
		public async Task GetActivityForGames_KeepsGameActivitySessions_OnDaysWithoutPlaytimeInsightsData()
		{
			// Arrange
			var gameId = Guid.NewGuid();
			var gameActivity = A.Fake<IGameActivityExtension>();
			var playtimeInsights = A.Fake<IGameActivityExtension>();
			A.CallTo(() => gameActivity.GetActivityForGames(A<IEnumerable<Game>>._)).Returns(new List<Activity>
			{
				CreateActivity(gameId, new DateTime(2024, 3, 1, 10, 0, 0), 100)
			});
			A.CallTo(() => playtimeInsights.GetActivityForGames(A<IEnumerable<Game>>._)).Returns(new List<Activity>
			{
				CreateActivity(gameId, new DateTime(2025, 3, 1, 10, 0, 0), 1000)
			});
			var sut = new CompositeActivityExtension(gameActivity, playtimeInsights);

			// Act
			var result = await sut.GetActivityForGames(new List<Game>());

			// Assert
			var activity = Assert.Single(result);
			Assert.Equal(2, activity.Items.Count);
			Assert.Contains(activity.Items, x => x.ElapsedSeconds == 100);
			Assert.Contains(activity.Items, x => x.ElapsedSeconds == 1000);
		}

		[Fact]
		public async Task GetActivityForGames_IncludesGamesThatOnlyHavePlaytimeInsightsSessions()
		{
			// Arrange
			var gameActivityGameId = Guid.NewGuid();
			var playtimeInsightsGameId = Guid.NewGuid();
			var gameActivity = A.Fake<IGameActivityExtension>();
			var playtimeInsights = A.Fake<IGameActivityExtension>();
			A.CallTo(() => gameActivity.GetActivityForGames(A<IEnumerable<Game>>._)).Returns(new List<Activity>
			{
				CreateActivity(gameActivityGameId, new DateTime(2025, 3, 1, 10, 0, 0), 100)
			});
			A.CallTo(() => playtimeInsights.GetActivityForGames(A<IEnumerable<Game>>._)).Returns(new List<Activity>
			{
				CreateActivity(playtimeInsightsGameId, new DateTime(2025, 3, 1, 10, 0, 0), 1000)
			});
			var sut = new CompositeActivityExtension(gameActivity, playtimeInsights);

			// Act
			var result = await sut.GetActivityForGames(new List<Game>());

			// Assert
			Assert.Equal(2, result.Count);
			Assert.Contains(result, x => x.Id == playtimeInsightsGameId);
		}

		private static Activity CreateActivity(Guid gameId, DateTime sessionStart, int elapsedSeconds)
		{
			return new Activity
			{
				Id = gameId,
				Name = $"Game {gameId}",
				Items = new List<Session>
				{
					new Session
					{
						DateSession = sessionStart,
						ElapsedSeconds = elapsedSeconds
					}
				}
			};
		}
	}
}
