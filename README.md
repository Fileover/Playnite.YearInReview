# Playnite Year In Review
![DownloadCountTotal](https://img.shields.io/github/downloads/sparrowbrain/playnite.yearinreview/total?label=total%20downloads&style=for-the-badge)
![LatestVersion](https://img.shields.io/github/v/release/SparrowBrain/Playnite.YearInReview?label=Latest%20version&style=for-the-badge)
![DownloadCountLatest](https://img.shields.io/github/downloads/SparrowBrain/Playnite.YearInReview/latest/total?style=for-the-badge)


## What is it?
Celebrate your last year of play by reviewing some of the play statistics from your Playnite game sessions!

![Main YearInReview view](/ci/screenshots/01.png)

## Requirements
* You will need to set you user name in the settings;
* ⚠ You need session data from **GameActivity** (mandatory for most setups, install it from the Playnite Add-On browser or here: https://playnite.link/addons.html#playnite-gameactivity-plugin) **or** from the [Playtime Insights](https://github.com/SHINKU1506/PlaytimeInsights) extension;
* GameActivity and Playtime Insights data are merged: sessions tracked by Playtime Insights win for days it covers, while GameActivity fills in everything else;
* You will need to have session data from previous years to see any reports.

## Sharing with friends
You can share your report with friends. To share:
* Click the share button at the top-right corner of the report;
* Select whether you want to include game cover images in the report;![Main YearInReview view](/ci/screenshots/02.png)
* Save the report as .json file;
* Send it to your friends;

And to import you friend's report:
* Click the import button at the top-right corner of any report;
* Select the .json file you received from your friend;
* Click Open;

You should now see your friend's report :).

## Installation
You can install it either from Playnite's addon browser, or from [the web addon browser](https://playnite.link/addons.html#SparrowBrain_YearInReview).

## Contributing
The structure of the code is experimental, so expect some mess. Most of the code is grouped by "domain" or feature.


### Unit Testing
Most of the code should be written with Unit Tests. It is preferable to apply TDD.

## Localization
You can help translate the extension to your language on the [Crowdin](https://crowdin.com/project/sparrowbrain-playnite-year-in-review) page.