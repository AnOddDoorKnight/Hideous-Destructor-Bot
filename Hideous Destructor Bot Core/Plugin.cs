using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer;

public interface IPlugin
{
	string Key { get; }

	IEnumerable<SocketApplicationCommand>? Commands => null;

	void AddFunctionality(Bot bot);
	Task UpdateFunctionality(Bot bot)
	{
		return Task.CompletedTask;
	}
	void RemoveFunctionality(Bot bot);
}