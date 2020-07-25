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

		private Regex IdRegex = new Regex("\\d", RegexOptions.Compiled);

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
				List<guid> userIds = this.Client.GetMentionedUserIds(e);
				ITextChannel channel = await FindOrCreateThread(userIds.First(), false);
				if( string.IsNullOrEmpty(e.TrimmedMessage) || userIds.Count != 1 || channel == null )
				{
					await e.SendReplySafe("A who?");
					return;
				}

				await e.SendReplySafe($"Thread open: <#{channel.Id}>");
				//todo confirmation (emoji?)
			};
			commands.Add(newCommand);

// !reply
			newCommand = new Command("reply");
			newCommand.Type = CommandType.Standard;
			newCommand.Description = "Reply in the current thread.";
			newCommand.ManPage = new ManPage("<message text>", "<message text> - Message that will be sent to the user.");
			newCommand.RequiredPermissions = PermissionType.ServerOwner | PermissionType.Admin;
			newCommand.OnExecute += async e => {
				List<guid> userIds = this.Client.GetMentionedUserIds(e);
				ITextChannel channel = await FindOrCreateThread(userIds.First(), false);
				if( string.IsNullOrEmpty(e.TrimmedMessage) && (!e.Message.Attachments?.Any() ?? false) )
				{
					await e.SendReplySafe("I can't send an empty message.");
					return;
				}

				Match match = this.IdRegex.Match(e.Channel.Topic);
				if( !match.Success || !guid.TryParse(match.Value, out guid userId) )
				{
					await e.SendReplySafe("This does not seem to be a modmail thread. This command can only be used in a modmail thread channel.");
					return;
				}

				Embed embed = GetMessageEmbed(e.Message);
				await SendModmailPm(e.Channel, userId, null, embed);
			};
			commands.Add(newCommand);

// !close
			newCommand = new Command("close");
			newCommand.Type = CommandType.Standard;
			newCommand.Description = "Close the current modmail thread.";
			newCommand.ManPage = new ManPage("", "");
			newCommand.RequiredPermissions = PermissionType.ServerOwner | PermissionType.Admin;
			newCommand.OnExecute += async e => {
				await CloseThread(e.Channel);
			};
			commands.Add(newCommand);

			return commands;
		}

		private async Task OnMessageReceived(SocketMessage message)
		{
			if( !(message.Channel is SocketDMChannel) ) //Not a PM
				return;

			ITextChannel channel = await FindOrCreateThread(message.Author.Id, true);
			if( channel == null )
				return;

			Embed embed = GetMessageEmbed(message);
			await channel.SendMessageAsync(embed: embed);
		}

		private async Task SendModmailPm(SocketTextChannel replyChannel, guid userId, string message, Embed embed = null)
		{
			SocketGuildUser user = null;
			foreach( Server server in this.Client.Servers.Values )
			{
				user = server.Guild.Users.FirstOrDefault(u => u.Id == userId);
				if( user != null )
					break;
			}

			string error = null;
			int result = await this.Client.SendPmSafe(user, message, embed);
			switch( result )
			{
				case 1:
					error = SentString;
					break;
				case 0:
					error = FailedPmString;
					break;
				case -1:
					error = DidntPmString;
					break;
				case -2:
					error = DiscordPoopString;
					break;
				default:
					error = UserNotFoundString;
					break;
			}

			if( !string.IsNullOrEmpty(error) )
				await replyChannel.SendMessageSafe("error");
			else if( embed != null )
				await replyChannel.SendMessageSafe("Message sent:", embed);
			else
				await replyChannel.SendMessageSafe($"Thread closed.");
		}

		private Embed GetMessageEmbed(IMessage message)
		{
			string footer = "Member";
			if( !(message.Author is SocketUser user) )
				return null;

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
			return GetMessageEmbed(user.GetNickname(), user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(), footer, message);
		}
		private Embed GetMessageEmbed(string name, string avatarUrl, string footer, IMessage message)
		{
			EmbedBuilder embedBuilder = new EmbedBuilder()
				.WithAuthor(new EmbedAuthorBuilder{IconUrl = avatarUrl, Name = name})
				.WithFooter(new EmbedFooterBuilder{Text = footer});

			if( !string.IsNullOrEmpty(message.Content) )
				embedBuilder.WithDescription(message.Content);

			if( message.Attachments?.Any() ?? false )
				embedBuilder.WithImageUrl(message.Attachments.First().Url);

			return embedBuilder.Build();
		}

		private async Task<ITextChannel> FindOrCreateThread(guid userId, bool sendUserInfo)
		{
			SocketGuildUser user = null;
			Server server = this.Client.Servers.Values.FirstOrDefault(s => (user = s.Guild.Users.FirstOrDefault(u => u.Id == userId)) != null);
			if( server == null || this.Client.Servers.ContainsKey(this.Client.Config.ModmailServerId) || (server = this.Client.Servers[this.Client.Config.ModmailServerId]) == null )
				return null;

			IChannel channel = server.Guild.Channels.FirstOrDefault(c => c is SocketTextChannel cc && (cc.Topic?.Contains(userId.ToString()) ?? false));
			if( channel == null )
			{
				channel = await server.Guild.CreateTextChannelAsync(user.GetUsername(), c => {
					c.Topic = $"UserId: {userId}";
					c.CategoryId = this.Client.Config.ModmailCategoryId;
				});
				if( sendUserInfo )
				{
					string message = null;
					if( !string.IsNullOrEmpty(this.Client.Config.ModmailNewThreadMessage) )
						 message = this.Client.Config.ModmailNewThreadMessage;
					Embed embed = GetUserInfoEmbed(user);
					((ITextChannel)channel)?.SendMessageAsync(message, embed: embed);
				}
			}

			return channel as ITextChannel;
		}

		private async Task CloseThread(SocketTextChannel channel)
		{
			Match match = this.IdRegex.Match(channel.Topic);
			if( !match.Success || !guid.TryParse(match.Value, out guid userId))
			{
				await channel.SendMessageAsync("This does not seem to be a modmail thread. This command can only be used in a modmail thread channel.");
				return;
			}

			//todo close thread

			await SendModmailPm(channel, userId, "Thread closed. You're welcome to send another message, should you wish to contact the moderators again.");
		}

		public Task Update(IValkyrjaClient iClient)
		{
			return Task.CompletedTask;
		}
	}
}
