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

//todo - simply explain the different config options.

