using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using JjjJjBot.Properties;

namespace JjjJjBot.Bot
{
  [XmlRoot("Config")]
  public class Config
  {
    public static Config BotConfig = new Config();

    private Config()
    {

    }
    [XmlElement("StartupMessage")]
    public long StartupMessage;
    [XmlElement("Server")]
    public List<Server> Servers = new List<Server>();
    [XmlElement("User")]
    public List<User> Users = new List<User>();

    public static bool MergeUsersSafeFromTelegram(string TelegramUsername, ulong DiscordId)
    {
      if (GetUser(null, DiscordId).TelegramUsername == TelegramUsername)
      {
        MergeUsers(TelegramUsername, DiscordId);
        return true;
      }
      GetUser(TelegramUsername, 0).DiscordId = DiscordId;
      Export();
      return false;
    }

    public static bool MergeUsersSafeFromDiscord(string TelegramUsername, ulong DiscordId)
    {
      if (GetUser(TelegramUsername, 0).DiscordId == DiscordId)
      {
        MergeUsers(TelegramUsername, DiscordId);
        return true;
      }
      GetUser(null, DiscordId).TelegramUsername = TelegramUsername;
      Export();
      return false;
    }

    public static void MergeUsers(string TelegramUsername, ulong DiscordId)
    {
      User usertel = GetUser(TelegramUsername, 0);
      User userdis = GetUser(null, DiscordId);
      BotConfig.Users.RemoveAll(user => user.TelegramUsername == TelegramUsername);
      BotConfig.Users.RemoveAll(user => user.DiscordId == DiscordId);
      if (usertel.Status < userdis.Status)
        usertel.Status = userdis.Status;
      usertel.DiscordId = userdis.DiscordId;
      BotConfig.Users.Add(usertel);
      Export();
    }

    public static Server GetServerByChanel(long TelegramId, ulong DiscordId)
    {
      ulong serverid = 0;
      foreach (var server in BotConfig.Servers)
      foreach (var chanel in server.Chanels)
        if (chanel.TelegramId == TelegramId || chanel.DiscordId == DiscordId)
          serverid = server.Id;
      return GetServer(serverid);
    }

    public static Chanel GetChanel(long TelegramId, ulong DiscordId)
    {
      return GetServerByChanel(TelegramId, DiscordId).GetChanel(TelegramId, DiscordId);
    }

    public static Server GetServer(ulong Id)
    {
      var r = BotConfig.Servers.Find(server => server.Id == Id);
      if (r == null)
      {
        var a = new Server { Id = Id };
        BotConfig.Servers.Add(a);
        return a;
      }
      return r;
    }

    public static User GetUser(string TelegramUsername, ulong DiscordId)
    {
      if (TelegramUsername != null)
      {
        var r = BotConfig.Users.Find(user => user.TelegramUsername == TelegramUsername);
        if (r != null)
          return r;
      }
      if (DiscordId != 0)
      {
        var r = BotConfig.Users.Find(user => user.DiscordId == DiscordId);
        if (r != null)
          return r;
      }
      var a = new User { TelegramUsername = TelegramUsername, DiscordId = DiscordId };
      BotConfig.Users.Add(a);
      Export();
      return a;
    }

    public class User
    {
      [XmlElement("TelegramId")]
      public int TelegramId;
      [XmlElement("TelegramUsername")]
      public string TelegramUsername;
      [XmlElement("DiscordId")]
      public ulong DiscordId;
      public enum UserStatus
      {
        [XmlEnum("Bot")]
        Bot = -1,
        [XmlEnum("Unknown")]
        Unknown = 0,
        [XmlEnum("Casual")]
        Casual = 1,
        [XmlEnum("Admin")]
        Admin = 1337,
        [XmlEnum("God")]
        God = 1409
      }

      [XmlElement("Status")]
      public UserStatus Status = UserStatus.Casual;
    }

    public class Server
    {
      [XmlElement("Id")]
      public ulong Id;
      [XmlElement("Chanel")]
      public List<Chanel> Chanels = new List<Chanel>();
      public Chanel GetChanel(long TelegramId, ulong DiscordId)
      {
        if (TelegramId != 0)
        {
          var r = Chanels.Find(chanel => chanel.TelegramId == TelegramId);
          if (r != null)
            return r;
        }
        if (DiscordId != 0)
        {
          var r = Chanels.Find(chanel => chanel.DiscordId == DiscordId);
          if (r != null)
            return r;
        }
        var a = new Chanel { TelegramId = TelegramId, DiscordId = DiscordId };
        Chanels.Add(a);
        Export();
        return a;
      }
      
      public void Merge(long TelegramId, ulong DiscordId)
      {
        Chanel tel = GetChanel(TelegramId, 0);
        foreach (var server in BotConfig.Servers)
        {
          server.Chanels.RemoveAll(chanel => chanel.TelegramId == TelegramId);
        }
        GetChanel(0, DiscordId).TelegramId = TelegramId;
        GetChanel(0, DiscordId).Type |= tel.Type;
        Export();
      }
    }

    public class Chanel
    {
      [XmlElement("DiscordId")]
      public ulong DiscordId;
      [XmlElement("TelegramId")]
      public long TelegramId;
      [XmlElement("Synchronize")]
      public bool Synchronize;

      [Flags]
      public enum ChanelType
      {
        [XmlEnum("Inactive")]
        Inactive = 0x0,
        [XmlEnum("Meme")]
        Meme = 0x1
      }

      [XmlElement("Type")]
      public ChanelType Type = ChanelType.Inactive;
    }

    private static Mutex FileIOMutex = new Mutex();

    public static void Import(string FileName = null)
    {
      FileIOMutex.WaitOne();
      if (FileName == null)
        FileName = Settings.Default.ConfigFile;
      XmlSerializer serializer = new XmlSerializer(typeof(Config));
      try
      {
        using (FileStream fileStream = new FileStream(FileName, FileMode.Open))
          BotConfig = (Config)serializer.Deserialize(fileStream);
      }
      catch
      {
        BotConfig = new Config();
      }

      FileIOMutex.ReleaseMutex();
    }

    public static void Export(string FileName = null)
    {
      FileIOMutex.WaitOne();
      if (FileName == null)
        FileName = Settings.Default.ConfigFile;
      XmlSerializer serializer = new XmlSerializer(typeof(Config));
      using (FileStream fileStream = new FileStream(FileName, FileMode.Create))
        serializer.Serialize(fileStream, BotConfig);
      FileIOMutex.ReleaseMutex();
    }

  }
}
