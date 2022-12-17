using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace HideousDestructor.DiscordServer.Plugins;

public sealed class MemeOfTheWeek : Plugin
{
	const string timeKey = "MemeOfTheWeek",
		winnerKey = "MemeOfTheWeek-Winners";
	// Do wednesday
	internal static bool PassedDay(GuildConfig state)
	{
		if (state.Contents.TryGetValue(timeKey, out string? value))
		{
			DateTime time = DateTime.Parse(value);
			TimeSpan span = DateTime.Today - time;
			if (span.TotalDays >= 7f)
				return false;
			return true;
		}
		SetToToday(state);
		return false;
	}
	private static void SetToToday(GuildConfig botState)
	{
		botState[timeKey] = DateTime.Today.ToString();
	}
	public SocketTextChannel Submissions { get; private set; }
	public SocketTextChannel Leaderboard { get; private set; }
	public IEmote Emote { get; private set; }
	//private readonly List<IMessage> messages = new();
	public MemeOfTheWeek(Bot bot, ulong guildID, ulong submissionsID, ulong leaderboardID) : base(bot, guildID)
	{
		Submissions = CurrentGuild.GetTextChannel(submissionsID);
		Leaderboard = CurrentGuild.GetTextChannel(leaderboardID);
		Emote = CurrentGuild.Emotes.First(emote => emote.Name == "upvote");
	}

	public override string Key => nameof(MemeOfTheWeek);

	protected internal override Task OnEnable(Bot bot)
	{
		bot.socketClient.MessageReceived += MessageRecieved;

		// Upvotes all previous messages
		Thread addReactions = new Thread(async () =>
		{
			List<Task> queuedMessages = new(byte.MaxValue);
			var enumerable = Submissions.GetMessagesAsync().GetAsyncEnumerator();
			while (await enumerable.MoveNextAsync())
			{
				IMessage[] messageCollection = enumerable.Current.ToArray();
				for (int i = 0; i < messageCollection.Length; i++)
				{
					// Manual wait limit
					await Task.Delay(1600);
					bool hasBot = messageCollection[i].GetReactionUsersAsync(Emote, int.MaxValue)
						.ToEnumerable().SelectMany(collection => collection.Select(user => user.Id))
						.ToHashSet().Contains(bot.socketClient.CurrentUser.Id);
					if (!hasBot)
					{
						Console.WriteLine($"Adding reaction to '{messageCollection[i]}'");
						queuedMessages.Add(messageCollection[i].AddReactionAsync(Emote));
					}
					else
						goto end;
				}
			}
			end:
			await Task.WhenAll(queuedMessages);
		});
		addReactions.Start();
		return Task.CompletedTask;
	}

	protected internal override async Task Update(Bot bot)
	{
		if (PassedDay(bot.Configs[CurrentGuild.Id]))
			return;
		await DoLeaderboard(bot);
	}

	public async Task DoLeaderboard(Bot bot)
	{
		int winners = int.Parse(bot.Configs[CurrentGuild.Id].GetOrDefault(winnerKey, "3"));
		new List<IMessage>(Leaderboard.GetMessagesAsync()
			.ToEnumerable().SelectMany(collection => collection)).ForEach(msg => msg.DeleteAsync().Wait());
		List<IMessage> allMessages = new(Submissions.GetMessagesAsync()
			.ToEnumerable().SelectMany(collection => collection)
			.OrderByDescending(message => message.Reactions[Emote].ReactionCount));
		for (int i = 0; i < Math.Min(allMessages.Count, winners); i++)
		{
			if (allMessages[i].Attachments.Count > 0)
			{
				var attachments = FileDownloader.GetImages(allMessages[i].Attachments).Result.AsAttachments();
				await Leaderboard.SendFilesAsync(attachments, $"**{i + 1}. {allMessages[i].Author.Username} with {allMessages[i].Reactions[Emote].ReactionCount - 1} votes**\n-----:\n\n{allMessages[i].Content}");
				attachments.DisposeAll();
			}
			else
				await Leaderboard.SendMessageAsync($"**{i + 1}. {allMessages[i].Author.Username} with {allMessages[i].Reactions[Emote].ReactionCount - 1} votes**\n-----:\n\n{allMessages[i].Content}");

		}
		SetToToday(bot.Configs[CurrentGuild.Id]);
		await Submissions.DeleteMessagesAsync(allMessages);
	}

	private async Task MessageRecieved(SocketMessage socketMessage)
	{
		if (socketMessage.Channel.Id != Submissions.Id)
			return;
		//await ((ITextChannel)socketMessage.Channel).CreateThreadAsync(null,  autoArchiveDuration: ThreadArchiveDuration.OneDay)
		//	.ContinueWith((task) => task.Result.SendMessageAsync("Ass"));
		await socketMessage.AddReactionAsync(Emote);

	}

}