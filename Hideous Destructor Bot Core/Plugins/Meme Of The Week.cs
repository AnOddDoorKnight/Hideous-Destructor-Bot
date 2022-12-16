using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Reflection;

namespace HideousDestructor.DiscordServer.Plugins;

public sealed class MemeOfTheWeek : IPlugin
{
	// Do wednesday
	public static bool PassedDay 
	{ 
		get
		{
			FileInfo fileInfo = GetFileInfo();
			if (fileInfo.Exists)
			{
				DateTime time = DateTime.Parse(File.ReadAllText(fileInfo.FullName));
				TimeSpan span = DateTime.Today - time;
				if (span.TotalDays >= 7f)
					return false;
				return true;
			}
			SetToToday(fileInfo);
			return false;
		}
	}
	private static void SetToToday(FileInfo info)
	{
		File.WriteAllText(info.FullName, DateTime.Today.ToString());
	}
	private static FileInfo GetFileInfo()
	{
		DirectoryInfo info = new(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
		info = info.CreateSubdirectory("BotData");
		return new FileInfo(info.FullName + "/state.txt");
	}

	public IEnumerable<SocketApplicationCommand>? Commands = new SocketApplicationCommand[]
	{

	};

	public SocketGuild CurrentGuild { get; private set; }
	public SocketTextChannel Submissions { get; private set; }
	public SocketTextChannel Leaderboard { get; private set; }
	public IEmote Emote { get; private set; }
	//private readonly List<IMessage> messages = new();
	public MemeOfTheWeek(Bot bot, ulong guildID, ulong submissionsID, ulong leaderboardID)
	{
		CurrentGuild = bot.socketClient.GetGuild(guildID);
		Submissions = CurrentGuild.GetTextChannel(submissionsID);
		Leaderboard = CurrentGuild.GetTextChannel(leaderboardID);
		Emote = CurrentGuild.Emotes.First(emote => emote.Name == "upvote");
	}

	public string Key => nameof(MemeOfTheWeek);

	public void AddFunctionality(Bot bot)
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
	}

	async Task IPlugin.UpdateFunctionality(Bot bot)
	{
		if (!PassedDay)
			PerformLeaderboard();
	}

	void IPlugin.RemoveFunctionality(Bot bot)
	{

	}

	private void PerformLeaderboard()
	{
		const int winners = 3;
		new List<IMessage>(Leaderboard.GetMessagesAsync()
			.ToEnumerable().SelectMany(collection => collection)).ForEach(msg => msg.DeleteAsync().Wait());
		List<IMessage> allMessages = new(Submissions.GetMessagesAsync()
			.ToEnumerable().SelectMany(collection => collection)
			.OrderByDescending(message => message.Reactions[Emote].ReactionCount));
		for (int i = 0; i < Math.Min(allMessages.Count, winners); i++)
		{
			Leaderboard.SendMessageAsync($"**{i + 1}. {allMessages[i].Author.Username} with {allMessages[i].Reactions[Emote].ReactionCount - 1} votes**{(allMessages[i].Attachments.Any() ? $"\n{allMessages[i].Attachments.First().Url}" : "")}\n-----:\n\n{allMessages[i].Content}").Wait();
		}
		SetToToday(GetFileInfo());
		allMessages.ForEach(msg => msg.DeleteAsync().Wait());
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