using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer;


public abstract class Plugin
{
	public abstract string Key { get; }
	public SocketGuild CurrentGuild { get; }

	public Plugin(Bot bot, ulong guildID)
	{
		CurrentGuild = bot.socketClient.GetGuild(guildID);
	}


	protected internal virtual Task OnEnable(Bot bot)
	{
		return Task.CompletedTask;
	}

	protected internal virtual Task Update(Bot bot)
	{
		return Task.CompletedTask;
	}

	protected internal virtual Task OnDisable(Bot bot)
	{
		return Task.CompletedTask;
	}
}