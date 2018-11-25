using Smod2;
using Smod2.Attributes;
using Smod2.Events;
using Smod2.EventHandlers;
using System.Collections.Generic;
using System.IO;
using System;
using Smod2.API;
using Microsoft.VisualBasic.FileIO;

namespace DonorTag
{
	[PluginDetails(
		author = "TheCreeperCow",
		name = "DonorTag",
		description = "Gives donors fancy tags",
		id = "com.thecreepercow.donortag",
		version = "4.2.10",
		SmodMajor = 3,
		SmodMinor = 1,
		SmodRevision = 17)]

	class DonorTagPlugin : Plugin
	{
		internal Dictionary<string, Tag> donorTags = new Dictionary<string, Tag>();

		public override void OnEnable()
		{
			this.donorTags = getDonorTags();
			this.Info("Loading tags into the server...");
			foreach (Tag tag in this.donorTags.Values)
			{
				this.Info(tag.ToString());
			}
			this.Info("Donor Tags successfully loaded.");
		}

		public override void OnDisable()
		{
		}

		public Dictionary<string, Tag> getDonorTags()
		{
			Dictionary<string, Tag> tags = new Dictionary<string, Tag>();
			if (!File.Exists("DonorTags.csv"))
			{
				//File.Create("DonorTags.csv");
				File.AppendAllText("DonorTags.csv", "discord,steam,rank,color,group" + Environment.NewLine);
				this.Debug("Created DonorTags.csv with header row: discord,steam,rank,color,group");
			}

			using (TextFieldParser reader = new TextFieldParser("DonorTags.csv"))
			{
				reader.TextFieldType = FieldType.Delimited;
				reader.SetDelimiters(",");
				reader.HasFieldsEnclosedInQuotes = true;
				List<String[]> rows = new List<String[]>();
				int counter = 0;
				while (!reader.EndOfData)
				{
					string[] donorParts = reader.ReadFields();
					if (counter == 0)
					{
						this.Debug("Skipping header row: " + string.Join(",", donorParts));
						continue;
					}
					
					if (donorParts.Length == 4)
					{
						tags[donorParts[1]] = new Tag(donorParts[0], donorParts[1], donorParts[2], donorParts[3], "");
						this.Debug("Adding tag: " + tags[donorParts[1]]);
					}
					else if (donorParts.Length == 5)
					{
						tags[donorParts[1]] = new Tag(donorParts[0], donorParts[1], donorParts[2], donorParts[3], donorParts[4]);
						this.Debug("Adding tag: " + tags[donorParts[1]]);
					}
					else
					{
						this.Warn("Invalid donor tag in configuration missing : " + string.Join(",", donorParts));
						continue;
					}
				}
			}
			return tags;
		}
		
		public override void Register()
		{
			this.AddEventHandler(typeof(IEventHandlerRoundStart), new RoundStartHandler(this), Priority.Highest);
			this.AddEventHandler(typeof(IEventHandlerPlayerJoin), new JoinHandler(this), Priority.Highest);
		}
	}

	struct Tag
	{
		public string discord, steam, rank, color, group;

		public Tag(string discord, string steam, string rank, string color, string group)
		{
			this.discord = discord;
			this.steam = steam;
			this.rank = rank;
			this.color = color;
			this.group = group;
		}

		public override string ToString()
		{
			return discord + "," + steam + "," + rank + "," + color + "," + group;
		}
	}

	class JoinHandler : IEventHandlerPlayerJoin
	{
		private DonorTagPlugin plugin;

		public JoinHandler(Plugin plugin)
		{
			this.plugin = (DonorTagPlugin) plugin;
		}

		public void OnPlayerJoin(PlayerJoinEvent ev)
		{
			if (ev.Player == null || ev.Player.SteamId == null)
			{
				plugin.Error("Player is null or the PlayerJoinEvent failed to pass the player's SteamID.");
				return;
			}
			
			if (this.plugin.donorTags.Count == 0)
			{
				this.plugin.Debug("Donor Tags array is empty. Populate it with tags.");
				this.plugin.donorTags = this.plugin.getDonorTags();
			}
			else
			{
				this.plugin.Debug("Using cached Donor Tags array for player.");
			}

			if (this.plugin.donorTags.ContainsKey(ev.Player.SteamId))
			{
				Tag tag = this.plugin.donorTags[ev.Player.SteamId];
				if (tag.group.Length > 0)
				{
					ev.Player.SetRank(tag.color, tag.rank, tag.group);
				}
				else
				{
					ev.Player.SetRank(tag.color, tag.rank);
				}
				this.plugin.Debug("Set tag for player: " + tag);
			}
		}
	}

	class RoundStartHandler : IEventHandlerRoundStart
	{
		private DonorTagPlugin plugin;

		public RoundStartHandler(Plugin plugin)
		{
			this.plugin = (DonorTagPlugin)plugin;
		}

		public void OnRoundStart(RoundStartEvent ev)
		{
			this.plugin.Info("Refreshing donor tags from configuration...");
			this.plugin.donorTags = this.plugin.getDonorTags();
			foreach (Player player in ev.Server.GetPlayers())
			{
				if (this.plugin.donorTags.ContainsKey(player.SteamId))
				{
					Tag tag = this.plugin.donorTags[player.SteamId];
					if (tag.group.Length > 0)
					{
						player.SetRank(tag.color, tag.rank, tag.group);
					}
					else
					{
						player.SetRank(tag.color, tag.rank);
					}
					this.plugin.Debug("Set tag for player: " + tag);
				}
			}
		}
	}
}
