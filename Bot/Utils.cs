using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using JjjJjBot.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace JjjJjBot.Bot
{
  public class Utils
  {
    private static List<string> KeyWords = new List<string>
    {
      "youtube",
      "youtu.be",
      "coub"
    };

    private static List<string> ImageEndings = new List<string>
    {
      "png",
      "jpg",
      "jpeg",
      "webp",
      "bmp"
    };

    private static List<string> AnimationEndings = new List<string>
    {
      "gif",
      "gifv"
    };

    private static List<string> VideoEndings = new List<string>
    {
      "webm",
      "mp4",
      "avi"
    };

    public class UrlInfo
    {
      public enum FileType
      {
        Image,
        Video,
        Animation,
        Link,
        GayBar
      }

      public FileType Type;

      public string Url;
    }

    public class ImgurResponse
    {
      public class Data
      {
        public string id;
        public string title;
        public string description;
        public ulong datetime;
        public string type;
        public bool animated;
        public int width;
        public int height;
        public int size;
        public int views;
        public int bandwidth;
        public string vote;
        public bool favorite;
        public bool nsfw;
        public string section;
        public string account_url;
        public string account_id;
        public string link;
        public bool is_ad;
        public bool in_most_viral;
        public bool has_sound;
      }

      public Data data;
      public bool success;
      public int status;
    }

    public UrlInfo GetUrlInfo(string Url)
    {
      UrlInfo info = new UrlInfo();
      foreach (string ending in ImageEndings)
        if (Url.Contains("." + ending))
        {
          info.Type = UrlInfo.FileType.Image;
          info.Url = Url;
          return info;
        }

      foreach (string ending in VideoEndings)
        if (Url.Contains("." + ending))
        { 
          info.Type = UrlInfo.FileType.Video;
          info.Url = Url;
          return info;
        }

      foreach (string ending in AnimationEndings)
        if (Url.Contains("." + ending))
        { 
          info.Type = UrlInfo.FileType.Animation;
          info.Url = Url;
          return info;
        }

      foreach (string keyWord in KeyWords)
        if (Url.Contains(keyWord))
        { 
          info.Type = UrlInfo.FileType.Link;
          info.Url = Url;
          return info;
        }

      if (Url.Contains("imgur"))
      {
        string apireq = "https://api.imgur.com/3/image/" + Url.Split('/').Last();
        WebRequest request = WebRequest.Create(apireq);
        request.Headers.Add("Authorization", "Client-ID 3f568dfd4116c39");
        var response = request.GetResponse();
        StreamReader reader =  new StreamReader(response.GetResponseStream());
        string json = reader.ReadToEnd();
        ImgurResponse data = JsonConvert.DeserializeObject<ImgurResponse>(json);
        if (data.data.animated)
        {
          if (data.data.has_sound)
            info.Type = UrlInfo.FileType.Video;
          else
            info.Type = UrlInfo.FileType.Animation;
        }
        else
          info.Type = UrlInfo.FileType.Image;
        info.Url = data.data.link;
        return info;
      }

      info.Type = UrlInfo.FileType.Link;
      info.Url = Url;

      return info;
    }

    public WebClient WebClient = new WebClient();

    public async Task Download(string Url, string Path, string FileName)
    {
      Directory.CreateDirectory(Path);
      await WebClient.DownloadFileTaskAsync(Url, Path + FileName);
    }
  }
}
