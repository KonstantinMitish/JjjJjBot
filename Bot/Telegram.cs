using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using JjjJjBot.Properties;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace JjjJjBot.Bot
{
  public class Telegram
  {
    TelegramBotClient Bot = new TelegramBotClient(Settings.Default.TelegramToken);

    private User Me;

    public event Func<string, Action<string>, Message, SocketMessage, ulong, Task> OnMessage;
    public event Func<Message, Task> OnChanel;

    public Telegram()
    {
      Bot.OnMessage += MessageReceived;
      Bot.OnUpdate += UpdateReceived;
      Me = Bot.GetMeAsync().GetAwaiter().GetResult();
    }

    public async Task<string> GetFile(string FileId, string Path, string FileName = null)
    {
      var f =  await Bot.GetFileAsync(FileId);
      if (Path != "")
        Directory.CreateDirectory(Path);
      string filename = Path + "/" + f.FileId + "." + f.FilePath.Split('.').Last();
      if (FileName != null)
        filename = Path + FileName;
      using (var profileImageStream = System.IO.File.Open(filename, FileMode.Create))
      {
        await Bot.DownloadFileAsync(f.FilePath, profileImageStream);
      }

      return filename;
    }

    private async void UpdateReceived(object sender, UpdateEventArgs e)
    {
      try
      {
        if (e.Update.Type == UpdateType.ChannelPost)
          if (OnChanel != null)
            await OnChanel(e.Update.ChannelPost);
      }
      catch (Exception exception)
      {
        Console.WriteLine(exception.Message);
      }
    }


    public long GetChanelId(string ChanelName)
    {
      var chat = Bot.GetChatAsync(ChanelName).GetAwaiter().GetResult();
      return chat.Id;
    }

    public async Task<bool> SendPicture(long ChatId, string FilePath)
    {
      try
      {
        await Bot.SendPhotoAsync(ChatId, FilePath);
      }
      catch
      {
        return false;
      }

      return true;
    }

    public async Task<bool> SendVideo(long ChatId, string FilePath)
    {
      try
      {
        await Bot.SendVideoAsync(ChatId, FilePath);
      }
      catch
      {
        return false;
      }

      return true;
    }

    public async Task<bool> SendAnimation(long ChatId, string FilePath)
    {
      try
      {
        await Bot.SendAnimationAsync(ChatId, FilePath);
      }
      catch
      {
        return false;
      }

      return true;
    }
    public async void SendTextMessage(long ChatId, string Message)
    {
      try
      {
        await Bot.SendTextMessageAsync(ChatId, Message);
      }
      catch { }
    }
   
    private async void MessageReceived(object sender, MessageEventArgs e)
    {
      try
      {
        
        Console.WriteLine(e.Message.From.Username + ":" + e.Message.Text);
        // ignore unknown, because Telegram is not support private bots
        //if (Config.GetUser(e.Message.From.Username, 0).Status == Config.User.UserStatus.Unknown)
        //  return;
        if (OnMessage != null)
          await OnMessage(e.Message.Text != null ? e.Message.Text : e.Message.Caption != null ? e.Message.Caption : "", s => { SendTextMessage(e.Message.Chat.Id, s); }, e.Message, null, 0);
      }
      catch (Exception exception)
      {
        SendTextMessage(e.Message.Chat.Id, "Bot failed successfully, please contact debil, kotoriy ego napisal and describe what was happened\n" + exception.Message);
      }
    }

    public void Stop()
    {
      Bot.StopReceiving();
    }

    public void Run()
    {
      Bot.StartReceiving();
    }
  }
}
