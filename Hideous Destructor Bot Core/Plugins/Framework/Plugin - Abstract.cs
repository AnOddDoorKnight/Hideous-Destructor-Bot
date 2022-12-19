using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer;

public abstract class ServerPlugin
{
	public static bool StartEnabled => false;
	public SocketGuild CurrentGuild { get; }

	public ServerPlugin(Bot bot, ulong guildID)
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
public abstract class GlobalPlugin
{
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