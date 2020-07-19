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

namespace Valkyrja.modules
{
	public class ExtraFeatures: IModule
	{
		private readonly Regex EmbedParamRegex = new Regex("--?\\w+.*?(?=\\s--?\\w|$)", RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
		private readonly Regex EmbedOptionRegex = new Regex("--?\\w+", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

		private IValkyrjaClient Client;

		public Func<Exception, string, guid, Task> HandleException{ get; set; }
		public bool DoUpdate{ get; set; } = true;

		public List<Command> Init(IValkyrjaClient iClient)
		{
			this.Client = iClient as ValkyrjaClient<BaseConfig>;
			List<Command> commands = new List<Command>();

// !embed
			Command newCommand = new Command("embed");
			newCommand.Type = CommandType.Standard;
			newCommand.Description = "Build an embed. Use without arguments for help.";
			newCommand.ManPage = new ManPage("<options>", "Use any combination of:\n" +
				"`--channel     ` - Channel where to send the embed.\n" +
				"`--edit <msgId>` - Replace a MessageId with a new embed (use after --channel)\n" +
				"`--title       ` - Title\n" +
				"`--description ` - Description\n" +
				"`--footer      ` - Footer\n" +
				"`--color       ` - #rrggbb hex color used for the embed stripe.\n" +
				"`--image       ` - URL of a Hjuge image in the bottom.\n" +
				"`--thumbnail   ` - URL of a smol image on the side.\n" +
				"`--fieldName   ` - Create a new field with specified name.\n" +
				"`--fieldValue  ` - Text value of a field - has to follow a name.\n" +
				"`--fieldInline ` - Use to set the field as inline.\n" +
				"Where you can repeat the field* options multiple times.");
			newCommand.RequiredPermissions = PermissionType.ServerOwner | PermissionType.Admin;
			newCommand.OnExecute += async e => {
				if( string.IsNullOrEmpty(e.TrimmedMessage) || e.TrimmedMessage == "-h" || e.TrimmedMessage == "--help" )
				{
					await e.SendReplySafe("```md\nCreate an embed using the following parameters:\n" +
					                      "[ --channel     ] Channel where to send the embed.\n" +
					                      "[ --edit <msgId>] Replace a MessageId with a new embed (use after --channel)\n" +
					                      "[ --title       ] Title\n" +
					                      "[ --description ] Description\n" +
					                      "[ --footer      ] Footer\n" +
					                      "[ --color       ] #rrggbb hex color used for the embed stripe.\n" +
					                      "[ --image       ] URL of a Hjuge image in the bottom.\n" +
					                      "[ --thumbnail   ] URL of a smol image on the side.\n" +
					                      "[ --fieldName   ] Create a new field with specified name.\n" +
					                      "[ --fieldValue  ] Text value of a field - has to follow a name.\n" +
					                      "[ --fieldInline ] Use to set the field as inline.\n" +
					                      "Where you can repeat the field* options multiple times.\n```"
					);
					return;
				}

				bool debug = false;
				IMessage msg = null;
				SocketTextChannel channel = e.Channel;
				EmbedFieldBuilder currentField = null;
				EmbedBuilder embedBuilder = new EmbedBuilder();

				foreach( Match match in this.EmbedParamRegex.Matches(e.TrimmedMessage) )
				{
					string optionString = this.EmbedOptionRegex.Match(match.Value).Value;

					if( optionString == "--fieldInline" )
					{
						if( currentField == null )
						{
							await e.SendReplySafe($"`fieldInline` can not precede `fieldName`.");
							return;
						}

						currentField.WithIsInline(true);
						if( debug )
							await e.SendReplySafe($"Setting inline for field `{currentField.Name}`");
						continue;
					}

					string value;
					if( match.Value.Length <= optionString.Length || string.IsNullOrWhiteSpace(value  = match.Value.Substring(optionString.Length + 1).Trim()) )
					{
						await e.SendReplySafe($"Invalid value for `{optionString}`");
						return;
					}

					if( value.Length >= BaseConfig.EmbedValueCharacterLimit )
					{
						await e.SendReplySafe($"`{optionString}` is too long! (It's {value.Length} characters while the limit is {BaseConfig.EmbedValueCharacterLimit})");
						return;
					}

					switch(optionString)
					{
						case "--channel":
							if( !guid.TryParse(value.Trim('<', '>', '#'), out guid id) || (channel = e.Server.Guild.GetTextChannel(id)) == null )
							{
								await e.SendReplySafe($"Channel {value} not found.");
								return;
							}
							if( debug )
								await e.SendReplySafe($"Channel set: `{channel.Name}`");

							break;
						case "--title":
							if( value.Length > 256 )
							{
								await e.SendReplySafe($"`--title` is too long (`{value.Length} > 256`)");
								return;
							}

							embedBuilder.WithTitle(value);
							if( debug )
								await e.SendReplySafe($"Title set: `{value}`");

							break;
						case "--description":
							if( value.Length > 2048 )
							{
								await e.SendReplySafe($"`--description` is too long (`{value.Length} > 2048`)");
								return;
							}

							embedBuilder.WithDescription(value);
							if( debug )
								await e.SendReplySafe($"Description set: `{value}`");

							break;
						case "--footer":
							if( value.Length > 2048 )
							{
								await e.SendReplySafe($"`--footer` is too long (`{value.Length} > 2048`)");
								return;
							}

							embedBuilder.WithFooter(value);
							if( debug )
								await e.SendReplySafe($"Description set: `{value}`");

							break;
						case "--image":
							try
							{
								embedBuilder.WithImageUrl(value.Trim('<', '>'));
							}
							catch( Exception )
							{
								await e.SendReplySafe($"`--image` is invalid url");
								return;
							}

							if( debug )
								await e.SendReplySafe($"Image URL set: `{value}`");

							break;
						case "--thumbnail":
							try
							{
								embedBuilder.WithThumbnailUrl(value.Trim('<', '>'));
							}
							catch( Exception )
							{
								await e.SendReplySafe($"`--thumbnail` is invalid url");
								return;
							}

							if( debug )
								await e.SendReplySafe($"Thumbnail URL set: `{value}`");

							break;
						case "--color":
							uint color = uint.Parse(value.TrimStart('#'), System.Globalization.NumberStyles.AllowHexSpecifier);
							if( color > uint.Parse("FFFFFF", System.Globalization.NumberStyles.AllowHexSpecifier) )
							{
								await e.SendReplySafe("Color out of range.");
								return;
							}

							embedBuilder.WithColor(color);
							if( debug )
								await e.SendReplySafe($"Color `{value}` set.");

							break;
						case "--fieldName":
							if( value.Length > 256 )
							{
								await e.SendReplySafe($"`--fieldName` is too long (`{value.Length} > 256`)\n```\n{value}\n```");
								return;
							}

							if( currentField != null && currentField.Value == null )
							{
								await e.SendReplySafe($"Field `{currentField.Name}` is missing a value!");
								return;
							}

							if( embedBuilder.Fields.Count >= 25 )
							{
								await e.SendReplySafe("Too many fields! (Limit is 25)");
								return;
							}

							embedBuilder.AddField(currentField = new EmbedFieldBuilder().WithName(value));
							if( debug )
								await e.SendReplySafe($"Creating new field `{currentField.Name}`");

							break;
						case "--fieldValue":
							if( value.Length > 1024 )
							{
								await e.SendReplySafe($"`--fieldValue` is too long (`{value.Length} > 1024`)\n```\n{value}\n```");
								return;
							}

							if( currentField == null )
							{
								await e.SendReplySafe($"`fieldValue` can not precede `fieldName`.");
								return;
							}

							currentField.WithValue(value);
							if( debug )
								await e.SendReplySafe($"Setting value:\n```\n{value}\n```\n...for field:`{currentField.Name}`");

							break;
						case "--edit":
							if( !guid.TryParse(value, out guid msgId) || (msg = await channel.GetMessageAsync(msgId)) == null )
							{
								await e.SendReplySafe($"`--edit` did not find a message with ID `{value}` in the <#{channel.Id}> channel.");
								return;
							}

							break;
						default:
							await e.SendReplySafe($"Unknown option: `{optionString}`");
							return;
					}
				}

				if( currentField != null && currentField.Value == null )
				{
					await e.SendReplySafe($"Field `{currentField.Name}` is missing a value!");
					return;
				}

				switch( msg )
				{
					case null:
						await channel.SendMessageAsync(embed: embedBuilder.Build());
						break;
					case RestUserMessage message:
						await message?.ModifyAsync(m => m.Embed = embedBuilder.Build());
						break;
					case SocketUserMessage message:
						await message?.ModifyAsync(m => m.Embed = embedBuilder.Build());
						break;
					default:
						await e.SendReplySafe("GetMessage went bork.");
						break;
				}
			};
			commands.Add(newCommand);

			return commands;
		}

		public Task Update(IValkyrjaClient iClient)
		{
			return Task.CompletedTask;
		}
	}
}
