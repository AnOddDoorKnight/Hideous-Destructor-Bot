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

public class DebugQueue : Plugin
{
	const float restartTime = 60f;
	const int breakPoint = 5;
	public List<LogMessage> logMessages = new();
	public Stopwatch updateTime = new();

	public SocketTextChannel ChannelTarget { get; private set; }

	public DebugQueue(Bot bot, ulong guild, ulong Channel) : base(bot, guild)
	{
		ChannelTarget = CurrentGuild.GetTextChannel(Channel);
	}

	public override string Key => nameof(DebugQueue);

	protected internal override async Task OnEnable(Bot bot)
	{
		bot.Log += msg =>
		{
			logMessages.Add(msg);
			return Task.CompletedTask;
		};
		await ChannelTarget.SendMessageAsync($"Connection Started on {DateTime.Now}! Running debugger.");
		updateTime.Start();
	}

	protected internal override async Task Update(Bot bot)
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
}
