using System;
using System.Collections.Generic;

namespace YearInReview.Extensions.PlaytimeInsights
{
	public class PlaytimeInsightsStore
	{
		public int SchemaVersion { get; set; }

		public DateTime UpdatedAtUtc { get; set; }

		public List<PlaytimeInsightsSession> Sessions { get; set; } = new List<PlaytimeInsightsSession>();
	}

	public class PlaytimeInsightsSession
	{
		public int SchemaVersion { get; set; }

		public Guid Id { get; set; }

		public Guid GameId { get; set; }

		public string GameName { get; set; } = string.Empty;

		public string GameSourceName { get; set; } = string.Empty;

		public string PlatformNames { get; set; } = string.Empty;

		public DateTime StartedAtUtc { get; set; }

		public DateTime EndedAtUtc { get; set; }

		public ulong ElapsedSeconds { get; set; }

		public int StartUtcOffsetMinutes { get; set; }

		public int EndUtcOffsetMinutes { get; set; }

		public string TimeZoneId { get; set; } = string.Empty;

		public bool ManuallyStopped { get; set; }

		public int Source { get; set; }

		public string RecoveryReason { get; set; } = string.Empty;

		public bool IsDeleted { get; set; }

		public DateTime? DeletedAtUtc { get; set; }

		public DateTime? LastModifiedAtUtc { get; set; }

		public string LastModifiedReason { get; set; } = string.Empty;

		public string ImportSource { get; set; } = string.Empty;

		public string ImportConfidence { get; set; } = string.Empty;
	}
}
