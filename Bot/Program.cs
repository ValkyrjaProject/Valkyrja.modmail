using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Valkyrja.coreLite;
using Valkyrja.entities;
using Valkyrja.modules;
using guid = System.UInt64;

namespace Valkyrja.modmail
{
	class Program
	{
		static void Main(string[] args)
		{
			string configPath = null;
			if( args != null && args.Length > 0 )
				configPath = args[0];

			int shardIdOverride = -1;
			if( args != null && args.Length > 1 && !int.TryParse(args[1], out shardIdOverride) )
			{
				Console.WriteLine("Invalid parameter.");
				return;
			}

			(new Client()).RunAndWait(shardIdOverride, configPath).GetAwaiter().GetResult();
		}
	}

	class Client
	{
		private ValkyrjaClient<Config> Bot;

		public Client()
		{}

		public async Task RunAndWait(int shardIdOverride = - 1, string configPath = null)
		{
			while( true )
			{
				this.Bot = new ValkyrjaClient<Config>(shardIdOverride, configPath);
				InitModules();

				try
				{
					await this.Bot.Connect();
					this.Bot.Events.Initialize += InitCommands;
					await Task.Delay(-1);
				}
				catch(Exception e)
				{
					await this.Bot.LogException(e, "--ValkyrjaClient crashed.");
					this.Bot.Dispose();
				}
			}
		}

		private void InitModules()
		{
			this.Bot.Modules.Add(new Modmail());
			this.Bot.Modules.Add(new ExtraFeatures());
		}

		private Task InitCommands()
		{
// !about
			Command newCommand = new Command("about");
			newCommand.Type = CommandType.Standard;
			newCommand.Description = "Display basic info about the bot.";
			newCommand.RequiredPermissions = PermissionType.Everyone;
			newCommand.OnExecute += async e => {
				await e.SendReplySafe("Valkyrja Modmail is an Open Source bot that enables private communication between the members of your community and the moderation team.\nYou can find the source code (and hosting information) at <https://github.com/ValkyrjaProject/Valkyrja.modmail> and get help at https://valkyrja.app/help");
			};
			this.Bot.Commands.Add(newCommand.Id.ToLower(), newCommand);

			return Task.CompletedTask;
		}
	}
}
