using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HideousDestructor.DiscordServer;

public readonly record struct ImageInfo(Image Image, string FileName);

public static class FileDownloader
{
	public static void DisposeAll<T>(this IEnumerable<T> values) where T : notnull
	{
		using var enumerator = values.GetEnumerator();
		if (typeof(T).GetInterface(nameof(IDisposable)) == null)
			throw new InvalidDataException($"There is no disposable for {typeof(T)}!");
		while (enumerator.MoveNext())
			((IDisposable)enumerator.Current).Dispose();
	}
	public static FileAttachment[] AsAttachments(this ImageInfo[] images)
	{
		var attachments = new FileAttachment[images.Length];
		for (int i = 0; i < images.Length; i++)
			attachments[i] = images[i].AsAttachment();
		return attachments;
	}
	public static FileAttachment AsAttachment(this ImageInfo image)
	{
		FileAttachment attachment = new(image.Image.Stream, image.FileName);
		return attachment;
	}

	public static Task<ImageInfo[]> GetImages(IEnumerable<IAttachment> urls)
	{
		return GetImages(urls.Select(attachment => attachment.Url).ToArray());
	}
	public static Task<ImageInfo[]> GetImages(params IAttachment[] urls)
	{
		return GetImages(urls.Select(attachment => attachment.Url).ToArray());
	}
	public static async Task<ImageInfo[]> GetImages(params string[] urls)
	{
		var imageStreams = new Task<byte[]>[urls.Length];
		var extensions = new string[urls.Length];
		var fileNames = new string[urls.Length];

		// Download it from online
		using HttpClient client = new();
		for (int i = 0; i < urls.Length; i++)
		{ 
			extensions[i] = urls[i].Substring(urls[i].LastIndexOf('.'));
			fileNames[i] = urls[i].Substring(urls[i].LastIndexOf('/'));
			imageStreams[i] = client.GetByteArrayAsync(urls[i]);
		}
		await Task.WhenAll(imageStreams);

		// Converting to a stream
		var images = new ImageInfo[imageStreams.Length];
		for (int i = 0; i < imageStreams.Length; i++)
		{
			MemoryStream stream = new(imageStreams[i].Result);
			images[i] = new ImageInfo(new Image(stream), fileNames[i] + extensions[i]);
		}
		return images;
	}
}