using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using System.Xml.Serialization;
using Discord;
using Discord.WebSocket;
using JjjJjBot.Bot;
using JjjJjBot.Properties;
using Telegram.Bot.Types;

namespace JjjJjBot.Bot
{
  public class Discord
  {
    private Dictionary<ulong, ulong> ReverceChanelMap = new Dictionary<ulong, ulong>();
    
    private DiscordSocketClient Client;

    public event Func<string, Action<string>, Message, IMessage, ulong, Task> OnMessage;

    public Discord()
    {
      Client = new DiscordSocketClient(new DiscordSocketConfig
      {
        LogLevel = LogSeverity.Info,
      });
      Client.Log += Log;
      Client.JoinedGuild += JoinedGuild;
      Client.Ready += Ready;
      Client.ChannelCreated += ChannelCreated;
      Client.MessageReceived += MessageReceived;
    }
    
    private async Task JoinedGuild(SocketGuild guild)
    {
      Console.WriteLine("Connected to server");
      Console.WriteLine("\t" + guild.Name);
      Console.WriteLine("\t\tChanels:");
      foreach (var channel in guild.Channels)
      {
        Console.WriteLine("\t\t\t" + channel.Name);
        ReverceChanelMap[channel.Id] = guild.Id;
      }
    }

    public async Task CallbackAllMessages(ulong ServerId, ulong ChanelId, int Interval = 0)
    {
      var m = await Client.GetGuild(ServerId).GetTextChannel(ChanelId).GetMessagesAsync(1000).Flatten();
        foreach (var message in m.Reverse())
        {
          if (OnMessage != null)
            try
          {
            // Ignore bots
            if (message.Author.IsBot)
              continue;
            if (message.ToString().StartsWith("/"))
              continue;
            await OnMessage(message.ToString(), s => { }, null, message, ServerId);
            }
            catch
            { }
          Thread.Sleep(Interval);
        }
    }

    public async Task SendMessage(ulong ServerId, ulong ChanelId, string Message)
    {
      await Client.GetGuild(ServerId).GetTextChannel(ChanelId).SendMessageAsync(Message);
    }

    public async Task SendFile(ulong ServerId, ulong ChanelId, string FileName, string Caption = "")
    {
      await Client.GetGuild(ServerId).GetTextChannel(ChanelId).SendFileAsync(FileName, Caption);
    }

    public ulong GetUserId(string Username, string Discriminant)
    {
      return Client.GetUser(Username, Discriminant).Id;
    }

    private async Task Ready()
    {
      Console.WriteLine("Available servers:");
      foreach (var guild in Client.Guilds)
      {
        Console.WriteLine("\t" + guild.Name);
        Console.WriteLine("\t\tChanels:");
        foreach (var channel in guild.Channels)
        {
          Console.WriteLine("\t\t\t" + channel.Name);
          ReverceChanelMap[channel.Id] = guild.Id;
        }
      }
    }

    private async Task MessageReceived(SocketMessage e)
    {
      try
      {
        // Ignore bots
        if (e.Author.IsBot)
          return;
        Console.WriteLine(e.Author.Username + ":" + e.ToString());
        ulong id = e.Channel.Id;
        ulong serverid = 0;
        if (ReverceChanelMap.ContainsKey(id))
          serverid = ReverceChanelMap[id];
        if (OnMessage != null)
          await OnMessage(e.ToString(), s => { e.Channel.SendMessageAsync(s); }, null, e, serverid);
      }
      catch (Exception exception)
      {
        await e.Channel.SendMessageAsync("Bot failed successfully, please contact debil, kotoriy ego napisal and describe what was happened\n" + exception.Message);
      }

    }

    private async Task ChannelCreated(SocketChannel e)
    {
      foreach (var guild in Client.Guilds)
        if (guild.Channels.Contains(e))
          ReverceChanelMap[e.Id] = guild.Id;
    }


    public async Task Run()
    {
      await Client.LoginAsync(TokenType.Bot, Settings.Default.DiscordToken);
      await Client.StartAsync();
    }

    public async Task Stop()
    {
      await Client.StopAsync();
    }

    private Task Log(LogMessage e)
    {
      Console.WriteLine(e.Message);
      return Task.CompletedTask;
    }
  }
}
