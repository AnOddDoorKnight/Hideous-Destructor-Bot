using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer;

public class DebugQueue : IPlugin
{
	const float restartTime = 60f;
	const int breakPoint = 5;
	public List<LogMessage> logMessages = new();
	public Stopwatch updateTime = new();

	public SocketGuild CurrentGuild { get; private set; }
	public SocketTextChannel ChannelTarget { get; private set; }

	public DebugQueue(Bot bot, ulong guild, ulong Channel)
	{
		CurrentGuild = bot.socketClient.GetGuild(guild);
		ChannelTarget = CurrentGuild.GetTextChannel(Channel);
	}

	public string Key => nameof(DebugQueue);

	public void AddFunctionality(Bot bot)
	{
		bot.Log += msg =>
		{
			logMessages.Add(msg);
			return Task.CompletedTask;
		};
		ChannelTarget.SendMessageAsync($"Connection Started on {DateTime.Now}! Running debugger.").Wait();
		updateTime.Start();
	}

	public async Task UpdateFunctionality(Bot bot)
	{
		if (updateTime.Elapsed.TotalSeconds < restartTime || logMessages.Count > breakPoint)
			return;
		updateTime.Stop();
		Task[] allTasks = new Task[logMessages.Count];
		for (int i = 0; i < allTasks.Length; i++)
			allTasks[i] = ChannelTarget.SendMessageAsync(logMessages[i].ToString());
		await Task.WhenAll(allTasks);
		updateTime.Restart();
	}

	public void RemoveFunctionality(Bot bot)
	{
		throw new NotImplementedException();
	}
}
