using System;
using System.Collections.Concurrent;
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

		private Regex IdRegex = new Regex("\\d+", RegexOptions.Compiled);
		private  readonly ConcurrentDictionary<guid, guid> ReplyMsgIds = new ConcurrentDictionary<guid, guid>();

		public List<Command> Init(IValkyrjaClient iClient)
		{
			this.Client = iClient as ValkyrjaClient<Config>;
			List<Command> commands = new List<Command>();

			this.Client.Events.MessageReceived += OnMessageReceived;
			this.Client.Events.MessageUpdated += OnMessageUpdated;

// !contact
			Command newCommand = new Command("contact");
			newCommand.Type = CommandType.Standard;
			newCommand.Description = "Create a new modmail thread with the mentioned user.";
			newCommand.ManPage = new ManPage("<userId>", "`<userId>` - User mention or ID with whom to initiate a discussion.");
			newCommand.RequiredPermissions = PermissionType.ServerOwner | PermissionType.Admin | PermissionType.Moderator | PermissionType.SubModerator;
			newCommand.OnExecute += async e => {
				List<guid> userIds = this.Client.GetMentionedUserIds(e);
				if( string.IsNullOrEmpty(e.TrimmedMessage) || userIds.Count != 1 )
				{
					await e.SendReplySafe("A who?");
					return;
				}

				ITextChannel channel = await FindOrCreateThread(userIds.First(), true, false);
				if( channel == null )
				{
					await e.SendReplySafe("Failed to create a channel.");
					return;
				}

				await e.SendReplySafe($"Thread open: <#{channel.Id}>");
			};
			commands.Add(newCommand);

// !reply
			newCommand = new Command("reply");
			newCommand.Type = CommandType.Standard;
			newCommand.DeleteRequest = true;
			newCommand.Description = "Reply in the current thread.";
			newCommand.ManPage = new ManPage("<message text>", "`<message text>` - Message that will be sent to the user.");
			newCommand.RequiredPermissions = PermissionType.ServerOwner | PermissionType.Admin | PermissionType.Moderator | PermissionType.SubModerator;
			newCommand.OnExecute += async e => {
				if( string.IsNullOrEmpty(e.TrimmedMessage) && (!e.Message.Attachments?.Any() ?? false) )
				{
					await e.SendReplySafe("I can't send an empty message.");
					return;
				}

				Match match = this.IdRegex.Match(e.Channel.Topic ?? "");
				if( !match.Success || !guid.TryParse(match.Value, out guid userId) )
				{
					await e.SendReplySafe("This does not seem to be a modmail thread. This command can only be used in a modmail thread channel.");
					return;
				}

				Embed embed = GetMessageEmbed(e.Message);
				await SendModmailPm(e, userId, null, embed);
			};
			commands.Add(newCommand);

// !anonReply
			newCommand = new Command("anonReply");
			newCommand.Type = CommandType.Standard;
			newCommand.DeleteRequest = true;
			newCommand.Description = "Reply in the current thread anonymously.";
			newCommand.ManPage = new ManPage("<message text>", "`<message text>` - Message that will be sent to the user.");
			newCommand.RequiredPermissions = PermissionType.ServerOwner | PermissionType.Admin | PermissionType.Moderator | PermissionType.SubModerator;
			newCommand.OnExecute += async e => {
				if( string.IsNullOrEmpty(e.TrimmedMessage) && (!e.Message.Attachments?.Any() ?? false) )
				{
					await e.SendReplySafe("I can't send an empty message.");
					return;
				}

				Match match = this.IdRegex.Match(e.Channel.Topic ?? "");
				if( !match.Success || !guid.TryParse(match.Value, out guid userId) )
				{
					await e.SendReplySafe("This does not seem to be a modmail thread. This command can only be used in a modmail thread channel.");
					return;
				}

				uint color = uint.Parse(this.Client.Config.ModmailEmbedColorMods.TrimStart('#'), System.Globalization.NumberStyles.AllowHexSpecifier);
				Embed embed = GetMessageEmbed("Moderators", e.Server.Guild.IconUrl, "Moderator", color, e.Message);
				await SendModmailPm(e, userId, null, embed);
			};
			commands.Add(newCommand);

// !close
			newCommand = new Command("close");
			newCommand.Type = CommandType.Standard;
			newCommand.Description = "Close the current modmail thread.";
			newCommand.ManPage = new ManPage("", "");
			newCommand.RequiredPermissions = PermissionType.ServerOwner | PermissionType.Admin | PermissionType.Moderator | PermissionType.SubModerator;
			newCommand.OnExecute += async e => {
				await CloseThread(e, e.CommandId.ToLower() == "close");
				await e.Message.AddReactionAsync(new Emoji("✅"));
			};
			commands.Add(newCommand);
			commands.Add(newCommand.CreateCopy("silentClose"));

			return commands;
		}

		private async Task OnMessageReceived(SocketMessage message)
		{
			if( message.Author.Id == this.Client.DiscordClient.CurrentUser.Id )
				return;
			if( !(message.Channel is SocketDMChannel) ) //Not a PM
				return;

			try
			{
				ITextChannel channel = await FindOrCreateThread(message.Author.Id, true, true);
				if( channel == null )
					return;

				Embed embed = GetMessageEmbed(message);
				await SendThreadReply(channel, message.Id, embed: embed);
				await message.AddReactionAsync(new Emoji("✅"));
			}
			catch( Exception e )
			{
				await this.HandleException(e, "OnMessageReceived", 0);
				await message.AddReactionAsync(new Emoji("❌"));
			}
		}

		private async Task OnMessageUpdated(IMessage originalMessage, SocketMessage updatedMessage, ISocketMessageChannel _)
		{
			if( originalMessage.Content == updatedMessage.Content )
				return;
			if( updatedMessage.Author.Id == this.Client.DiscordClient.CurrentUser.Id )
				return;
			if( !(updatedMessage.Channel is SocketDMChannel) ) //Not a PM
				return;

			try
			{
				await updatedMessage.RemoveAllReactionsAsync();
				ITextChannel channel = await FindOrCreateThread(updatedMessage.Author.Id, true, true);
				if( channel == null )
					return;

				Embed embed = GetMessageEmbed(updatedMessage);
				await SendThreadReply(channel, updatedMessage.Id, embed: embed);
				await updatedMessage.AddReactionAsync(new Emoji("✅"));
			}
			catch( Exception e )
			{
				await this.HandleException(e, "OnMessageReceived", 0);
				await updatedMessage.AddReactionAsync(new Emoji("❌"));
			}
		}

		private async Task SendModmailPm(CommandArguments commandArgs, guid userId, string message, Embed embed = null)
		{
			SocketGuildUser user = null;
			foreach( Server server in this.Client.Servers.Values )
			{
				user = server.Guild.Users.FirstOrDefault(u => u.Id == userId);
				if( user != null )
					break;
			}

			if( user == null )
			{
				await commandArgs.SendReplySafe($"User <@{userId}> not found.");
				return;
			}
			try
			{
				await user.SendMessageSafe(message, embed);

				if( embed != null )
					await commandArgs.SendReplySafe("Message sent:", embed);
				else
					await commandArgs.SendReplySafe("Thread closed.");
			}
			catch( HttpException e ) when( (int)e.HttpCode == 403 || (e.DiscordCode.HasValue && e.DiscordCode == 50007) || e.Message.Contains("50007") )
			{
				await commandArgs.SendReplySafe("I was unable to send the PM - they have disabled PMs from server members!");
			}
			catch( HttpException e ) when( (int)e.HttpCode >= 500 )
			{
				await commandArgs.SendReplySafe("I was unable to send the PM - received Discord Server Error 500 - please try again.");
			}
			catch( Exception e )
			{
				await this.HandleException(e, "SendModmailPm", user.Guild.Id);
				await commandArgs.SendReplySafe("Unknown error.");
			}
		}

		private async Task SendThreadReply(ITextChannel channel, guid msgId, string message = null, Embed embed = null, AllowedMentions allowedMentions = null)
		{
			//await this.Client.LogMessage(LogType.Response, this.Channel, this.Client.GlobalConfig.UserId, message);
			if( message == null && embed == null )
				return;

			if( this.ReplyMsgIds.ContainsKey(msgId) )
			{
				if( await channel.GetMessageAsync(this.ReplyMsgIds[msgId]) is SocketUserMessage msg )
				{
					await msg.ModifyAsync(m => {
						m.Content = message;
						m.Embed = embed;
					});
					return;
				}
			}

			IUserMessage reply = await channel.SendMessageAsync(message, embed: embed, allowedMentions: allowedMentions);
			this.ReplyMsgIds.TryAdd(msgId, reply.Id);
		}

		private Embed GetMessageEmbed(IMessage message)
		{
			string footer = "Member";
			uint color = uint.Parse(this.Client.Config.ModmailEmbedColorMembers.TrimStart('#'), System.Globalization.NumberStyles.AllowHexSpecifier);
			if( !(message.Author is SocketUser user) )
				return null;

			foreach( SocketGuild guild in user.MutualGuilds )
			{
				Server server;
				if( !this.Client.Servers.ContainsKey(guild.Id) || (server = this.Client.Servers[guild.Id]) == null )
					continue;

				SocketGuildUser guildUser = guild.GetUser(user.Id);
				if( server.IsModerator(guildUser) || server.IsSubModerator(guildUser) )
				{
					footer = "Moderator";
					color = uint.Parse(this.Client.Config.ModmailEmbedColorMods.TrimStart('#'), System.Globalization.NumberStyles.AllowHexSpecifier);
				}

				if( server.IsAdmin(guildUser) )
				{
					footer = "Admin";
					color = uint.Parse(this.Client.Config.ModmailEmbedColorAdmins.TrimStart('#'), System.Globalization.NumberStyles.AllowHexSpecifier);
				}

				if( footer != "Member" && !string.IsNullOrEmpty(this.Client.Config.ModmailFooterOverride) )
					footer = this.Client.Config.ModmailFooterOverride;
			}

			string name = user.GetUsername();
			string avatarUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl();
			return GetMessageEmbed(name, avatarUrl, footer, color, message);
		}
		private Embed GetMessageEmbed(string name, string avatarUrl, string footer, uint color, IMessage message)
		{
			EmbedBuilder embedBuilder = new EmbedBuilder()
				.WithAuthor(new EmbedAuthorBuilder{IconUrl = avatarUrl, Name = name})
				.WithTimestamp(DateTimeOffset.UtcNow)
				.WithFooter(new EmbedFooterBuilder{Text = footer})
				.WithColor(color);

			string messageText = message.Content;
			if( message.Content.ToLower().StartsWith($"{this.Client.CoreConfig.CommandPrefix}reply") )
				messageText = message.Content.Substring($"{this.Client.CoreConfig.CommandPrefix}reply".Length);
			if( message.Content.ToLower().StartsWith($"{this.Client.CoreConfig.CommandPrefix}anonreply") )
				messageText = message.Content.Substring($"{this.Client.CoreConfig.CommandPrefix}anonReply".Length);

			if( message.Attachments?.Any() ?? false )
			{
				string first = message.Attachments.First().Url;
				if( first.ToLower().EndsWith(".jpg") || first.ToLower().EndsWith(".jpeg") || first.ToLower().EndsWith(".png") || first.ToLower().EndsWith(".gif") || first.ToLower().EndsWith(".bmp") || first.ToLower().EndsWith(".webp") )
					embedBuilder.WithImageUrl(message.Attachments.First().Url);
				else messageText += $"\n\n {first}";
			}

			if( !string.IsNullOrEmpty(messageText) )
				embedBuilder.WithDescription(messageText);

			return embedBuilder.Build();
		}

		private Embed GetUserInfoEmbed(SocketGuildUser user)
		{
			EmbedBuilder embedBuilder = new EmbedBuilder()
				.WithAuthor(new EmbedAuthorBuilder{IconUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(), Name = user.GetUsername()})
				.WithTimestamp(Utils.GetTimeFromId(user.Id))
				.WithFooter(new EmbedFooterBuilder{Text = "Account created: " + Utils.GetTimestamp(Utils.GetTimeFromId(user.Id))})
				.AddField("Username", $"`{user.GetUsername()}`", true)
				.AddField("User ID", $"`{user.Id}`", true)
				.AddField("Roles", $"{user.Roles.Select(r => r.Name.Replace('`', '\'')).ToNames()}", false);

			return embedBuilder.Build();
		}

		private async Task<ITextChannel> FindOrCreateThread(guid userId, bool sendUserInfo, bool sendCustomMessage = false)
		{
			SocketGuildUser user = null;
			Server server = this.Client.Servers.Values.FirstOrDefault(s => (user = s.Guild.Users.FirstOrDefault(u => u.Id == userId)) != null);
			if( server == null || !this.Client.Servers.ContainsKey(this.Client.Config.ModmailServerId) || (server = this.Client.Servers[this.Client.Config.ModmailServerId]) == null )
				return null;

			IChannel channel = server.Guild.Channels.FirstOrDefault(c => c is SocketTextChannel cc && (cc.Topic?.Contains(userId.ToString()) ?? false));
			if( channel == null )
			{
				channel = await server.Guild.CreateTextChannelAsync(user.GetUsername().Replace('#', '-'), c => {
					c.Topic = $"UserId: {userId}";
					c.CategoryId = this.Client.Config.ModmailCategoryId;
				});
				if( sendUserInfo )
				{
					string message = null;
					if( sendCustomMessage && !string.IsNullOrEmpty(this.Client.Config.ModmailNewThreadMessage) )
						 message = this.Client.Config.ModmailNewThreadMessage;
					Embed embed = GetUserInfoEmbed(user);
					((ITextChannel)channel)?.SendMessageAsync(message, embed: embed);
				}
			}
			else
			{
				SocketGuildChannel socketGuildChannel = channel as SocketGuildChannel;
				await socketGuildChannel.ModifyAsync(c => c.CategoryId = this.Client.Config.ModmailCategoryId);
			}


			return channel as ITextChannel;
		}

		private async Task CloseThread(CommandArguments commandArgs, bool notify = true)
		{
			Match match = this.IdRegex.Match(commandArgs.Channel.Topic);
			if( !match.Success || !guid.TryParse(match.Value, out guid userId))
			{
				await commandArgs.SendReplySafe("This does not seem to be a modmail thread. This command can only be used in a modmail thread channel.");
				return;
			}

			await commandArgs.Channel.ModifyAsync(c => c.CategoryId = this.Client.Config.ModmailArchiveCategoryId);

			SocketCategoryChannel category = commandArgs.Channel.Guild.GetCategoryChannel(this.Client.Config.ModmailArchiveCategoryId);
			while( (category?.Channels.Count ?? 0) > this.Client.Config.ModmailArchiveLimit )
			{
				SocketGuildChannel oldChannel = category.Channels.OrderBy(c => c.Id).FirstOrDefault();
				if( oldChannel == null )
					break;
				await oldChannel.DeleteAsync();
			}

			if( notify )
				await SendModmailPm(commandArgs, userId, "Thread closed. You're welcome to send another message, should you wish to contact the moderators again.");
		}

		public Task Update(IValkyrjaClient iClient)
		{
			return Task.CompletedTask;
		}
	}
}
