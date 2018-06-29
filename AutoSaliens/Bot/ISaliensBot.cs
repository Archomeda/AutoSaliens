using System;

namespace AutoSaliens.Bot
{
    internal interface ISaliensBot
    {
        event EventHandler BotActivated;

        event EventHandler BotDeactivated;


        bool IsBotActive { get; }

        ILogger Logger { get; set; }


        void Start();

        void Stop();
    }
}
