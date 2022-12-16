using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer.Plugins;

public class Startup : IPlugin
{
	public SocketGuild CurrentGuild { get; private set; }
	public Startup(Bot bot, ulong guildID)
	{
		CurrentGuild = bot.socketClient.GetGuild(guildID);
	}

	public string Key => throw new NotImplementedException();

	public void AddFunctionality(Bot bot)
	{

		//bot.socketClient.CreateGlobalApplicationCommandAsync()
		bot.socketClient.SlashCommandExecuted += async (command) =>
		{
			await command.RespondAsync("I am alive, thanks for checking! Rock and Stone!");
		};
	}

	public void RemoveFunctionality(Bot bot)
	{
		
	}
}