Author: Radka Gustavsson, [rhea.dev](https://rhea.dev)


## The Valkyrja
Please take a look at our website to see what's the bot about, full list of features, invite and configuration: [http://valkyrja.app](http://valkyrja.app)

## Contributors:

* Please read the [Contributing file](CONTRIBUTING.md) before you start =)
* Fork this repository, and then clone it recursively to get the Core library as well: `git clone --recursive git@github.com:YOURUSERNAME/Valkyrja.modmail.git`
* Nuke your nuget cache `dotnet nuget locals all --clear`

## Project structure

The Valkyrja project is split into these repositories:
* `Valkyrja.coreLite` - Core client code.
* `Valkyrja.modmail` - Modmail by Valkyrja.

## Hosted instance

Valkyrja Project offers hosting of this bot for its Patreon subscribers and any other Open Source oriented community or project. (This is subject to change from Patreon to GH-Sponsors in the near future.)

## Self hosting

You will need to [create a Discord Bot.](https://discordpy.readthedocs.io/en/latest/discord.html)

### Docker / Podman

//todo

### Linux

1) Install .NET Core preferably using your package manager, e.g: `$ sudo dnf install dotnet` on Fedora.
2) Clone this repository recursively: `git clone --recursive git@github.com:ValkyrjaProject/Valkyrja.modmail.git`
3) Run it with `dotnet run` to create an empty `config.json`
4) Move the `config.json` into the `bin/Release/` (you need to create these folders first) and then modify it to configure the the bot - more details in the __Configuration__ section.
5) Copy the example systemd unit file `systemd-example.service` into the `/etc/systemd/system/` path: `$ sudo cp systemd-example.service /etc/systemd/system/modmail.service`
6) Modify this file to reflect the correct path to the project, based on wherever you cloned it.
7) Enable and start the service: `$ sudo systemctl enable --now modmail`

### Windows

//todo [Issue #1](https://github.com/ValkyrjaProject/Valkyrja.modmail/issues/1)

## Configuration

### Bot config file

`config.json` contains these properties with their default values:
`"ModmailServerId" = 0` - ID of a server on which you wish to have modmail threads.
`"ModmailCategoryId" = 0` - Main modmail category for active threads.
`"ModmailArchiveCategoryId" = 0` - Archive category where closed threads live.
`"ModmailArchiveLimit" = 5` - Oldest threads will be deleted after exceeding this number of closed threads in the Archive.
`"ModmailFooterOverride" = ""` - Defaults to configured roles for Admin or Moderator if left empty.
`"ModmailNewThreadMessage" = ""` - Used to ping the staff for newly created threads - use raw mention format: `<@&roleid>`
`"ModmailEmbedColorAdmins" = "#ff0000"` - Embed color for Admin messages.
`"ModmailEmbedColorMods" = "#0000ff"` - Embed color for Moderator messages.
`"ModmailEmbedColorMembers" = "#00ff00"` - Embed color for all other member messages.

`"DiscordToken" = "asdfasdfasdf"` - Your Discord token goes here, don't forget the quotes.
`"GameStatus" = "PM me to reach Mods!"` - The "Playing..." status message.
`"CommandPrefix" = "!"` - How the commands will be triggered.
`"OwnerUserId" = 0` - User ID of the administrator of this bot.
`"NotificationChannelId" = 0` - This channel may be used to notify the team of possible errors.
`"AdminRoleIds" = [0]` - A list of role IDs that will have Admin permissions.
`"ModeratorRoleIds" = [0]` - A list of role IDs that will have Moderator permissions.
`"SubModeratorRoleIds" = [0]` - A list of role IDs that will have SubModerator permissions.

You are advised to leave everything else with its default value.

### Discord configuration

You need to create the Modmail and Modmail Archive categories yourself, and allow the bot to correctly function in these.

We recommend that you give it a hoisted role with merely Read Messages permission so that the bot can be displayed in all channels, and then configure these two categories to have the following permissions:

`Manage Channel`, `Read & See Channels`, `Send Messages`, `Manage Messages`, `Embed Links`, `Attach Files`, `Read Message History`, `Add Reactions`

