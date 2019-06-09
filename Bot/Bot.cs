using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using Discord;
using Telegram.Bot;
using Telegram.Bot.Args;
using Discord.WebSocket;
using JjjJjBot.Properties;
using Telegram.Bot.Types;
using MessageType = Telegram.Bot.Types.Enums.MessageType;

namespace JjjJjBot.Bot
{
  public class Bot
  {
    Discord Discord = new Discord();
    Telegram Telegram = new Telegram();
    Utils Utils = new Utils();
    public Bot()
    {
      Config.Import();
      Telegram.OnMessage += OnMessage;
      Telegram.OnChanel += OnChanel;
      Discord.OnMessage += OnMessage;
    }

    private async Task OnChanel(Message e)
    {
      if (Config.GetChanel(e.Chat.Id, 0).Synchronize)
        await Syncronize(e, null);
    }

    private bool RunFlag = true;

    public async Task Run()
    {
      Telegram.Run();
      await Discord.Run();
      Telegram.SendTextMessage(Config.BotConfig.StartupMessage, Settings.Default.StartupMessage);
      while (RunFlag) 
        await Task.Delay(1000);
      Telegram.Stop();
      Discord.Stop().Wait();
      Config.Export();
    }

    private async Task Syncronize(Message TelegramHandle, IMessage DiscordHandle)
    {
      if (DiscordHandle != null)
      {
        Uri tryurl;// = new Uri("https://i.ytimg.com/vi/JWtBr9V8KJU/maxresdefault.jpg");
        if (Uri.TryCreate(DiscordHandle.ToString(), UriKind.Absolute, out tryurl))
        {
          string url = tryurl.ToString();
          var info = Utils.GetUrlInfo(url);
          switch (info.Type)
          {
            case Utils.UrlInfo.FileType.Image:
              await Telegram.SendPicture(Config.GetChanel(0, DiscordHandle.Channel.Id).TelegramId, info.Url);
              break;
            case Utils.UrlInfo.FileType.Animation:
              await Telegram.SendAnimation(Config.GetChanel(0, DiscordHandle.Channel.Id).TelegramId, info.Url);
              break;
            case Utils.UrlInfo.FileType.Video:
              await Telegram.SendVideo(Config.GetChanel(0, DiscordHandle.Channel.Id).TelegramId, info.Url);
              break;
            case Utils.UrlInfo.FileType.Link:
              Telegram.SendTextMessage(Config.GetChanel(0, DiscordHandle.Channel.Id).TelegramId, info.Url);
              break;
          }
        }
        foreach (var attachment in DiscordHandle.Attachments)
        {
          if (Settings.Default.SaveContent)
          {
            string path = Settings.Default.DownloadsPath + "/" + DiscordHandle.Channel.Id + "/" + DiscordHandle.Id +
                          "/" +
                          attachment.Id + "/";
            await Utils.Download(attachment.Url, path, attachment.Filename);
          }

          var info = Utils.GetUrlInfo(attachment.Url);
          switch (info.Type)
          {
            case Utils.UrlInfo.FileType.Image:
              await Telegram.SendPicture(Config.GetChanel(0, DiscordHandle.Channel.Id).TelegramId, attachment.Url);
              break;
            case Utils.UrlInfo.FileType.Animation:
              await Telegram.SendAnimation(Config.GetChanel(0, DiscordHandle.Channel.Id).TelegramId, attachment.Url);
              break;
            case Utils.UrlInfo.FileType.Video:
              await Telegram.SendVideo(Config.GetChanel(0, DiscordHandle.Channel.Id).TelegramId, attachment.Url);
              break;
            case Utils.UrlInfo.FileType.Link:
              Telegram.SendTextMessage(Config.GetChanel(0, DiscordHandle.Channel.Id).TelegramId, attachment.Url);
              break;
          }
        }
      }

      if (TelegramHandle != null)
      {
        string FilePath = null;
        var server = Config.GetServerByChanel(TelegramHandle.Chat.Id, 0);
        switch (TelegramHandle.Type)
        {
          case MessageType.Text:
            await Discord.SendMessage(server.Id, server.GetChanel(TelegramHandle.Chat.Id, 0).DiscordId, TelegramHandle.Text);
            return;
          case MessageType.Audio:
            FilePath = await Telegram.GetFile(TelegramHandle.Audio.FileId, Settings.Default.DownloadsPath);
            break;
          case MessageType.Photo:
            FilePath = await Telegram.GetFile(TelegramHandle.Photo.LastOrDefault()?.FileId, Settings.Default.DownloadsPath);
            break;
          case MessageType.Video:
            FilePath = await Telegram.GetFile(TelegramHandle.Video.FileId, Settings.Default.DownloadsPath);
            break;
          case MessageType.Document:
            FilePath = await Telegram.GetFile(TelegramHandle.Document.FileId, Settings.Default.DownloadsPath);
            break;
          case MessageType.Voice:
            FilePath = await Telegram.GetFile(TelegramHandle.Voice.FileId, Settings.Default.DownloadsPath);
            break;
          case MessageType.VideoNote:
            FilePath = await Telegram.GetFile(TelegramHandle.VideoNote.FileId, Settings.Default.DownloadsPath);
            break;
          case MessageType.Sticker:
            FilePath = await Telegram.GetFile(TelegramHandle.Sticker.FileId, Settings.Default.DownloadsPath);
            break;
        }

        if (FilePath != null)
        {
          await Discord.SendFile(server.Id, server.GetChanel(TelegramHandle.Chat.Id, 0).DiscordId, FilePath);
        }
      }
    }

    private async Task OnMessage(string message, Action<string> ResponseFucntion, Message TelegramHandle,
      IMessage DiscordHandle, ulong DiscordServerId)
    {
      string TelegramUsername = null;
      ulong DiscordId = 0;
      if (TelegramHandle != null)
        TelegramUsername = TelegramHandle.From.Username;
      if (DiscordHandle != null)
        DiscordId = DiscordHandle.Author.Id;
      var user = Config.GetUser(TelegramUsername, DiscordId);
      // Superuser id confirmation
      if (user.Status == Config.User.UserStatus.God &&
          TelegramHandle != null &&
          user.TelegramId != TelegramHandle.From.Id)
      {
        ResponseFucntion("It's impossible! ACCESS DENIED");
        return;
      }

      var trimmed = message.ToLower().Trim();
      if (trimmed.StartsWith("/"))
      {
        await ProcessCommand(message, ResponseFucntion, TelegramHandle, DiscordHandle, DiscordServerId, user.Status);
        return;
      }
      
      if (DiscordHandle != null)
      {
        if (Config.GetChanel(0, DiscordHandle.Channel.Id).Synchronize)
          await Syncronize(TelegramHandle, DiscordHandle);
      }
    }

    private void Restart(int Delay = 5000)
    {
      Process process = new Process();
      ProcessStartInfo startInfo = new ProcessStartInfo();
      startInfo.WindowStyle = ProcessWindowStyle.Normal;
      startInfo.FileName = Settings.Default.Runner;
      startInfo.Arguments = Process.GetCurrentProcess().ProcessName + ".exe temp " + Delay;
      process.StartInfo = startInfo;
      process.Start();
      RunFlag = false;
    }

    private async Task ProcessCommand(string message, Action<string> ResponseFucntion, Message TelegramHandle, IMessage DiscordHandle, ulong DiscordServerId, Config.User.UserStatus AccessLevel )
    {
      try
      {
        try
        {
          string[] args = message.Trim().Split(' ');
          switch (args[0].ToLower())
          {
            case "/help":
              string argumentsexamplechanel = "";
              if (TelegramHandle != null)
                argumentsexamplechanel = "@chanel";
              ResponseFucntion("жъжъь_bot: \n" +
                               "/save - Force save config\n" +
                               "/reload - Reload config \n" +
                               "/sync_history - Upload chanel history\n" +
                               $"/setmemechanel {argumentsexamplechanel} - Mark chanel as meme\n" +
                               $"/ruinmemechanel {argumentsexamplechanel} - Unmark chanel as meme\n" +
                               "/setadmin <user> - Make user as admin\n" +
                               "/setcasual <user> - Make user as gay\n" +
                               "/merge <your login in other system> - Merge your Telegram & Discord accounts, мне лень писать инструкцию к этому\n" +
                               "/mergeusers - Merge users (admin)\n" +
                               "/mergechanel @chanel - Merge current chanel with telegram (Discord only)\n" +
                               $"/sync {argumentsexamplechanel} <true/false> - Make current chanel sync, вот и нахуй ты это всё прочитал?\n");
              break;
            case "/update":
              if (AccessLevel < Config.User.UserStatus.God)
                throw new Exception("Access denied");

              if (TelegramHandle != null)
              {
                Telegram.GetFile(TelegramHandle.Document.FileId, "", "temp").Wait();
                ResponseFucntion("Restarting in 5 seconds...");
                Restart();
              }
              break;
            case "/reload":
              if (AccessLevel < Config.User.UserStatus.Admin)
                throw new Exception("Access denied");
              Config.Import();
              ResponseFucntion("Config reloaded");
              break;
            case "/sync_history":
              if (AccessLevel < Config.User.UserStatus.Admin)
                throw new Exception("Access denied");
              if (DiscordHandle != null)
              {
                bool arg = false;
                if (Config.GetServer(DiscordServerId).GetChanel(0, DiscordHandle.Channel.Id).TelegramId == 0)
                  throw new Exception("This chanel is not linked to Telegram");

                if (Config.GetServer(DiscordServerId).GetChanel(0, DiscordHandle.Channel.Id).Synchronize == false)
                  throw new Exception("This chnel is not synchronized");
                ResponseFucntion("Нуче, погнали нахуй!");
                int interval = 0;
                if (args.Length > 1)
                  interval = int.Parse(args[1]);
                await Discord.CallbackAllMessages(DiscordServerId, DiscordHandle.Channel.Id, interval);
              }
              if (TelegramHandle != null)
              {
                ResponseFucntion("Ебучий апи телеги не дает доступа к истории каналов, так что соси хуй");
              }
              break;
            case "/setmemechanel":
              if (AccessLevel < Config.User.UserStatus.Admin)
                throw new Exception("Access denied");
              Config.Chanel chat = new Config.Chanel();
              if (DiscordHandle != null)
                chat = Config.GetServer(DiscordServerId).GetChanel(0, DiscordHandle.Channel.Id);
              else if (TelegramHandle != null)
              {
                long chatid = Telegram.GetChanelId(args[1]);
                chat = Config.GetServer(DiscordServerId).GetChanel(chatid, 0);
              }
              if (chat.Type == Config.Chanel.ChanelType.Meme)
                ResponseFucntion("This chanel is already Tracer(meme)");
              else
              {
                Config.GetServer(DiscordServerId).GetChanel(chat.TelegramId, chat.DiscordId).Type = Config.Chanel.ChanelType.Meme;
                Config.Export();
                ResponseFucntion("This chanel is now meme");
              }
              break;
            case "/ruinmemechanel":
              if (AccessLevel < Config.User.UserStatus.Admin)
                throw new Exception("Access denied");
              chat = new Config.Chanel();
              if (DiscordHandle != null)
                chat = Config.GetServer(DiscordServerId).GetChanel(0, DiscordHandle.Channel.Id);
              else if (TelegramHandle != null)
              {
                long chatid = Telegram.GetChanelId(args[1]);
                chat = Config.GetServer(DiscordServerId).GetChanel(chatid, 0);
              }
              if (chat.Type == Config.Chanel.ChanelType.Inactive)
                ResponseFucntion("This chanel is already Tracer(not meme)");
              else
              {
                Config.GetServer(DiscordServerId).GetChanel(chat.TelegramId, chat.DiscordId).Type = Config.Chanel.ChanelType.Inactive;
                Config.Export();
                ResponseFucntion("This chanel is now not meme");
              }
              break;
            case "/setadmin":
              if (AccessLevel < Config.User.UserStatus.Admin)
                throw new Exception("Access denied");
              if (args[1].StartsWith("@"))
                args[1] = args[1].Substring(1);
              if (Config.GetUser(args[1], 0).Status > Config.GetUser(TelegramHandle.From.Username, 0).Status)
                throw new Exception("You can't edit user higher than you");
              Config.GetUser(args[1], 0).Status = Config.User.UserStatus.Admin;
              Config.Export();
              ResponseFucntion($"@{args[1]} is now admin");
              break;
            case "/setcasual":
              if (AccessLevel < Config.User.UserStatus.Admin)
                throw new Exception("Access denied");
              if (args[1].StartsWith("@"))
                args[1] = args[1].Substring(1);
              if (Config.GetUser(args[1], 0).Status > Config.GetUser(TelegramHandle.From.Username, 0).Status)
                throw new Exception("You can't edit user higher than you");
              Config.GetUser(args[1], 0).Status = Config.User.UserStatus.Casual;
              Config.Export();
              ResponseFucntion($"@{args[1]} is now casual");
              break;
            case "/merge":
              if (AccessLevel < Config.User.UserStatus.Casual)
                throw new Exception("Access denied");
              if (DiscordHandle != null)
              {
                if (Config.MergeUsersSafeFromDiscord(args[1], DiscordHandle.Author.Id))
                  ResponseFucntion("Successfully merged with Telegram");
                else
                  ResponseFucntion("Merge request added. Please do the same in Telegram");
              }
              if (TelegramHandle != null)
              {
                if (args.Length < 2)
                  throw new IndexOutOfRangeException();
                try
                {
                  string[] user = args[1].Trim().Split('#');
                  if (Config.MergeUsersSafeFromTelegram(TelegramHandle.From.Username, Discord.GetUserId(user[0].Trim(), user[1].Trim())))
                    ResponseFucntion("Successfully merged with Discord");
                  else
                    ResponseFucntion("Merge request added. Please do the same in Discord");
                }
                catch
                {
                  throw new Exception("User not found");
                }
              }
              break;
            case "/mergeusers":
              if (AccessLevel < Config.User.UserStatus.Admin)
                throw new Exception("Access denied");
              string[] userdis;
              string usertel;
              if (args[1].Contains('#'))
              {
                userdis = args[1].Trim().Split('#');
                usertel = args[2];
              }
              else
              {
                userdis = args[2].Trim().Split('#');
                usertel = args[1];
              }
              Config.MergeUsers(usertel, Discord.GetUserId(userdis[0], userdis[1]));
              ResponseFucntion($"Successfully merged {args[1]} and {args[2]}");
              break;
            case "/mergechanel":
              if (AccessLevel < Config.User.UserStatus.Admin)
                throw new Exception("Access denied");
              if (DiscordHandle != null)
              {
                Config.GetServer(DiscordServerId).Merge(Telegram.GetChanelId(args[1]), DiscordHandle.Channel.Id);
                ResponseFucntion("Successfully merged with Telegram");
              }
              if (TelegramHandle != null)
              {
                ResponseFucntion("Unavailable in Telegram");
              }
              break;
            case "/sync":
              if (AccessLevel < Config.User.UserStatus.Admin)
                throw new Exception("Access denied");
              if (DiscordHandle != null)
              {
                bool arg = false;
                if (Config.GetServer(DiscordServerId).GetChanel(0, DiscordHandle.Channel.Id).TelegramId == 0)
                  throw new Exception("This chanel is not linked to Telegram");
                if (args[1] == "true")
                  arg = true;
                else if (args[1] != "false")
                  throw new Exception($"Unknown word: {args[1]}");
                Config.GetServer(DiscordServerId).GetChanel(0, DiscordHandle.Channel.Id).Synchronize = arg;
                Config.Export();
                if (arg)
                  ResponseFucntion("This chanel is now synchronized");
                else
                  ResponseFucntion("This chanel is now not synchronized");
              }
              if (TelegramHandle != null)
              {
                bool arg = false;
                long telegramid = Telegram.GetChanelId(args[1]);
                if (Config.GetChanel(telegramid, 0).DiscordId == 0)
                  throw new Exception("This chanel is not linked to Discord");
                if (args[2] == "true")
                  arg = true;
                else if (args[2] != "false")
                  throw new Exception($"Unknown word: {args[1]}");
                Config.GetChanel(telegramid, 0).Synchronize = arg;
                Config.Export();
                if (arg)
                  ResponseFucntion("This chanel is now synchronized");
                else
                  ResponseFucntion("This chanel is now not synchronized");
              }
              break;
            case "/save":
              if (AccessLevel < Config.User.UserStatus.Admin)
                throw new Exception("Access denied");
              Config.Export();
              ResponseFucntion("Config saved");
              break;
            default:
              throw new Exception("Unknown command");
          }
        }
        catch (IndexOutOfRangeException)
        {
          throw new Exception("Ты параметры то укажи, еблан");
        }
      }
      catch (Exception exception)
      {
        ResponseFucntion("Command failed successfully: " + exception.Message);
      }
    }
  }
}
