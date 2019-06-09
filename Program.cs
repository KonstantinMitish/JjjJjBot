using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JjjJjBot.Bot;

namespace JjjJjBot
{
    class Program
    {
        static void Main(string[] args)
        {
          new Bot.Bot().Run().Wait();
        }
    }
}
