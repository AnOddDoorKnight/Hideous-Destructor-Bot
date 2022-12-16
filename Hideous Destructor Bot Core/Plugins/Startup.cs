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
	public SocketTextChannel ChannelTarget { get; private set; }
	public Startup(Bot bot, ulong guildID)
	{
		CurrentGuild = bot.socketClient.GetGuild(guildID);
		ChannelTarget = CurrentGuild.GetTextChannel(734127505723621417);
	}

	public string Key => throw new NotImplementedException();

	public void AddFunctionality(Bot bot)
	{
		ChannelTarget.SendMessageAsync(DateTime.Now.ToString()).Wait();
	}

	public void RemoveFunctionality(Bot bot)
	{
		
	}
}