# CarrotBot
This is a Discord bot.
It is not well-written or particularly useful.
People still use it, though, so it continues to run.

If you want to use it, too, you can add it at [this link](https://discord.bots.gg/bots/389513870835974146).

Additionally, you can join the server for support and testing at [this link](https://discord.gg/wHPwHu7).

To build and run your own instance of CarrotBot, you will need the following things:
* An AES256 encryption key at the source root(DSharpPlus/CarrotBot). This will be packaged into the bot executable.
* A Discord bot token, encrypted with AES256 with your encryption key, stored at the location from which the bot will be run, with the name `00_token.cb`.
  * To encrypt this token, you may use the `AES256WriteFile` function found in `DSharpPlus/CarrotBot/SensitiveInformation.cs`. The bot's source code can easily be temporarily modified in order to generate this file.
* (Optionally, to enable the `catpic` command) An API key for [thecatapi.com], encrypted in the same way, and stored at `00_cat-api-key.cb` in the same location.
