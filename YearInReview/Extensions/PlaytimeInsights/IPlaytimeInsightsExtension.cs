using System.Collections.Generic;
using System.Threading.Tasks;
using Playnite.SDK.Models;
using YearInReview.Extensions.GameActivity;

namespace YearInReview.Extensions.PlaytimeInsights
{
	public interface IPlaytimeInsightsExtension : IGameActivityExtension
	{
		bool IsDataAvailable();
	}
}
