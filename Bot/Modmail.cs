using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Valkyrja.coreLite;
using Valkyrja.entities;
using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using guid = System.UInt64;

namespace Valkyrja.modmail
{
	public class Modmail: IModule
	{
		private ValkyrjaClient<Config> Client;

		public Func<Exception, string, guid, Task> HandleException{ get; set; }
		public bool DoUpdate{ get; set; } = true;

		public List<Command> Init(IValkyrjaClient iClient)
		{
			this.Client = iClient as ValkyrjaClient<Config>;
			List<Command> commands = new List<Command>();

			this.Client.Events.MessageReceived += OnMessageReceived;

// !contact
			Command newCommand = new Command("contact");
			newCommand.Type = CommandType.Standard;
			newCommand.Description = "Create a new modmail thread with the mentioned user.";
			newCommand.ManPage = new ManPage("<userId>", "<userId> - User mention or ID with whom to initiate a discussion.");
			newCommand.RequiredPermissions = PermissionType.ServerOwner | PermissionType.Admin;
			newCommand.OnExecute += async e => {
				//todo
				await e.SendReplySafe("Not yet implemented.");
			};
			commands.Add(newCommand);

			return commands;
		}

		private async Task OnMessageReceived(SocketMessage message)
		{
			if( !(message.Channel is SocketDMChannel) ) //Not a PM
				return;

			//todo
		}

		private Embed GetMessageEmbed(SocketUser user)
		{
			string footer = "Member";
			foreach( SocketGuild guild in user.MutualGuilds )
			{
				Server server;
				if( !this.Client.Servers.ContainsKey(guild.Id) || (server = this.Client.Servers[guild.Id]) == null )
					continue;

				SocketGuildUser guildUser = guild.GetUser(user.Id);
				if( server.IsModerator(guildUser) || server.IsSubModerator(guildUser) )
					footer = "Moderator";
				if( server.IsAdmin(guildUser) )
					footer = "Admin";
				if( footer != "Member" && !string.IsNullOrEmpty(this.Client.Config.ModmailFooterOverride) )
					footer = this.Client.Config.ModmailFooterOverride;
			}
			return GetMessageEmbed(user.GetNickname(), footer, user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
		}
		private Embed GetMessageEmbed(string name, string footer, string avatarUrl)
		{
			EmbedBuilder embedBuilder = new EmbedBuilder();
			//todo

			return embedBuilder.Build();
		}

		public Task Update(IValkyrjaClient iClient)
		{
			return Task.CompletedTask;
		}
	}
}
