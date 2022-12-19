using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer;

internal sealed class SlashMessageHandler
{
	private readonly List<GuildSlashCommandInfo> GuildCommands = new();
	private readonly List<GlobalSlashCommandInfo> GlobalCommands = new();

	public SlashMessageHandler(Bot bot)
	{
		bot.socketClient.SlashCommandExecuted += UnblockInvoke;
	}

	private readonly List<Task> tasks = new();
	private Task UnblockInvoke(SocketSlashCommand command)
	{
		_ = Invoke(command);
		return Task.CompletedTask;
	}
	internal async Task Invoke(SocketSlashCommand command)
	{
		TaskCompletionSource completion = new();
		for (int i = 0; i < GuildCommands.Count; i++)
		{
			if (completion.Task.IsCompleted)
				goto end;
			async Task GuildTask(int index)
			{
				if (!GuildCommands[index].Predicate(command))
					return;
				await GuildCommands[index].Action(command);
				completion.SetResult();
				return;
			}
			tasks.Add(GuildTask(i));
		}
		for (int i = 0; i < GlobalCommands.Count; i++)
		{
			if (completion.Task.IsCompleted)
				goto end;
			async Task GlobalTask(int index)
			{
				if (!GlobalCommands[index].Predicate(command))
					return;
				await GlobalCommands[index].Action(command);
				completion.SetResult();
				return;
			}
			tasks.Add(GlobalTask(i));
		}
		await Task.WhenAny(completion.Task, Task.WhenAll(tasks));
	end:
		tasks.Clear();
	}
	public async Task AddListener(GuildSlashCommandInfo info)
	{
		await info.Bot.socketClient.GetGuild(info.GuildID).CreateApplicationCommandAsync(info.Build());
		GuildCommands.Add(info);
	}
	public async Task AddListener(GlobalSlashCommandInfo info)
	{
		await info.Bot.socketClient.CreateGlobalApplicationCommandAsync(info.Build());
		GlobalCommands.Add(info);
	}
}
public record struct GuildSlashCommandInfo(SlashCommandBuilder Builder, Func<SocketSlashCommand, Task> Action, Bot Bot, ulong GuildID)
{
	public SlashCommandProperties Build() => Builder.Build();
	/// <summary>
	/// Checks if it is the same command and subcommand
	/// </summary>
	public bool Predicate(SocketSlashCommand msg)
	{
		if (msg.CommandName != Builder.Name)
			return false;
		if (Bot.socketClient.GetGuild(GuildID).GetChannel(msg.ChannelId!.Value) == null)
			return false;
		if (Builder.Options != null && Builder.Options.Any())
		{
			using var enumerator = Builder.Options.TakeWhile(option => option.Type == ApplicationCommandOptionType.SubCommand).GetEnumerator();
			SocketSlashCommandDataOption[] data = msg.Data.Options.ToArray();
			for (int i = 0; enumerator.MoveNext(); i++)
			{
				if (data[i].Type != ApplicationCommandOptionType.SubCommand)
					return false;
				if (data[i].Name != enumerator.Current.Name)
					return false;
			}
		}
		return true;
	}
}
public record struct GlobalSlashCommandInfo(SlashCommandBuilder Builder, Func<SocketSlashCommand, Task> Action, Bot Bot)
{
	public SlashCommandProperties Build() => Builder.Build();
	/// <summary>
	/// Checks if it is the same command and subcommand
	/// </summary>
	public bool Predicate(SocketSlashCommand msg)
	{
		if (msg.CommandName != Builder.Name)
			return false;
		if (Builder.Options != null && Builder.Options.Any())
		{
			using var enumerator = Builder.Options.TakeWhile(option => option.Type == ApplicationCommandOptionType.SubCommand).GetEnumerator();
			SocketSlashCommandDataOption[] data = msg.Data.Options.ToArray();
			for (int i = 0; enumerator.MoveNext(); i++)
			{
				if (data[i].Type != ApplicationCommandOptionType.SubCommand)
					return false;
				if (data[i].Name != enumerator.Current.Name)
					return false;
			}
		}
		return true;
	}
}