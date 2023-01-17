using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer;

internal class AutoUpdateThread : IAsyncDisposable
{
	public readonly int baseDelay, additiveDelay;
	private readonly Thread updateThread;
	private readonly CancellationTokenSource cancellation = new();
	public AutoUpdateThread(Bot bot, int baseDelay, int additiveDelay)
	{
		this.baseDelay = baseDelay;
		this.additiveDelay = additiveDelay;
		updateThread = 
			new Thread(async () =>
			{
				List<Task> allTasks = new();
				while (!cancellation.IsCancellationRequested)
				{
					int globalCount = await bot.GlobalPlugins.Update();
					using IEnumerator<GuildManager> serverEnumerator = bot.Guilds.Select(coll => coll.Value)
						.GetEnumerator();
					while (serverEnumerator.MoveNext())
						allTasks.Add(serverEnumerator.Current.Update());
					await Task.WhenAll(allTasks);
					int updateCount = allTasks.Count + globalCount;
					allTasks.Clear();
					Thread.Sleep(baseDelay + (additiveDelay * updateCount));
				}
			})
			{
				Priority = ThreadPriority.Lowest,
			};
		updateThread.Start();
	}

	public ValueTask DisposeAsync()
	{
		return new ValueTask(Task.Run(async () =>
		{
			cancellation.Cancel();
			await Task.Run(() => SpinWait.SpinUntil(() => updateThread.ThreadState == ThreadState.Stopped));
			cancellation.Dispose();
		}));
	}
}
