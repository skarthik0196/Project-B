# Project-B

## Introduction
Project-B (To be named) is a WIP bot written in C# with Discord.NET with various functions. 

Currently Project-B includes
- **Anime Module**: Search for anime on MAL interactively, the API may be rate limited until a better caching mechanism is implemented
- **Azur Lane Module** : Display a myraid of information from the game Azur Lane including but not limited to ships (characters), skins, equipment, voicelines etc.

Planned:
- Update Anime module to search for manga, characters and people. Store user MAL ID's to display lists, compare user lists with leaderboards for anime/manga watched and so on.
- Calendar/Reminder Module to create events and tag the associated parties as reminders at the requested time.

## Build Instructions
- Clone the repo
- Setup your own bot as mentioned [here](https://docs.stillu.cc/guides/getting_started/first-bot.html) on the Discord developer portal and add it to your server.
- Create an environment variable with the name `ProjectBToken` and set the value to your Discord bot token.
- Open the solution file in Visual Studio and build.
- Run the project and you should the bot come online.

## Dependencies
Library dependencies are handled automatically through NuGet. Currently the following libraries are used
- [Discord.Net](https://github.com/discord-net/Discord.Net)
- [Jikan.NET](https://github.com/Ervie/jikan.net)
- [AzurAPINet](https://github.com/AzurAPI/AzurAPINet)
