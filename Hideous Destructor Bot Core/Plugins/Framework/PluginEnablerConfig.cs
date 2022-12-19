using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HideousDestructor.DiscordServer.IO;

/// <summary>
/// This class checks what plugins are enabled in the guild.
/// </summary>
public sealed class PluginEnablerConfig : IDisposable
{
	public const string pluginFileName = "ActivePlugins";
	public XmlDocument Document { get; private set; } = new();
	public FileInfo IOLocation { get; }
	public bool AutoFlush { get; set; } = true;

	public PluginEnablerConfig(ulong guildID)
	{
		DirectoryInfo guildInfo = DataGroup.GetSpecificGuildDirectory(guildID);
		IOLocation = new FileInfo($"{guildInfo.FullName}\\{pluginFileName}.xml");
		if (!IOLocation.Exists)
		{
			using XmlWriter writer = XmlWriter.Create(IOLocation.FullName, DataGroup.Settings);
			writer.WriteStartElement("ActivePlugins");
			writer.WriteEndElement();
			writer.Flush();
		}
		Document.Load(IOLocation.FullName);
	}

	public bool? this[string pluginKey]
	{
		get
		{
			var list = Document.LastChild!.ChildNodes;
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i]!.Name == pluginKey)
					return bool.Parse(list[i]!.InnerText);
			}
			return null;
		}
		set
		{
			var list = Document.LastChild!.ChildNodes;
			if (value == null)
			{
				bool? output = this[pluginKey];
				// Checking if its both null.
				if (output == null)
					return;
				// Removing the node
				for (int i = 0; i < list.Count; i++)
					if (list[i]!.Name == pluginKey)
					{
						Document.LastChild!.RemoveChild(list[i]!);
						goto autoFlush;
					}
				return;
			}
			// Modifying the value
			for (int i = 0; i < list.Count; i++)
				if (list[i]!.Name == pluginKey)
				{
					list[i]!.InnerText = value.Value.ToString();
					goto autoFlush;
				}
			// Creating the value if it doesn't exist
			var element = Document.CreateElement(pluginKey);
			element.InnerText = value.Value.ToString();
			Document.LastChild!.AppendChild(element);
		autoFlush:
			if (AutoFlush)
				Dispose();
		}
	}

	public void Dispose()
	{
		using XmlWriter writer = XmlWriter.Create(IOLocation.FullName, DataGroup.Settings);
		Document.WriteTo(writer);
		writer.Flush();
	}
}
