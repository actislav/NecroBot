#region using directives

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using PoGo.NecroBot.Logic.Common;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.Logging;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Tasks;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.WebSocket;

#endregion

namespace PoGo.NecroBot.CLI
{
    public class WebSocketInterface : ILogger
    {
        //private PokeStopListEvent _lastPokeStopList;
        //private ProfileEvent _lastProfile;
        private readonly WebSocketServer _server;
        private ISession _session;
        private LogLevel _maxLogLevel;
        private Queue<Tuple<DateTime, string>> _messages = new Queue<Tuple<DateTime, string>>(20);

        public WebSocketInterface(int port)
        {
            _server = new WebSocketServer();
            var setupComplete = _server.Setup(new ServerConfig
            {
                Name = "NecroWebSocket",
                Ip = "Any",
                Port = port,
                Mode = SocketMode.Tcp,
                Security = "tls",
                Certificate = new CertificateConfig
                {
                    FilePath = @"cert.pfx",
                    Password = "necro"
                }
            });

            if (setupComplete == false)
            {

                Logger.Write(TranslationString.WebSocketFailStart + $" with port: {port}", LogLevel.Error);
                return;
            }

            _server.NewMessageReceived += HandleMessage;
            _server.NewSessionConnected += HandleSession;

            _server.Start();
        }

        private void Broadcast(string message, DateTime messageTime)
        {
            if (_messages.Count > 20)
                _messages.Dequeue();

            _messages.Enqueue(new Tuple<DateTime, string>(messageTime.ToUniversalTime(), message));

            foreach (var session in _server.GetAllSessions())
            {
                SendMessage(session, message, messageTime.ToUniversalTime());
            }
        }

        private void SendMessage(WebSocketSession session, string message, DateTime messageTime)
        {
            try
            {
                DateTime lastMessageTime;
                if (DateTime.TryParseExact(session.Cookies?["LastMessageTime"] ?? DateTime.MinValue.ToString("hh:mm:ss.fff", CultureInfo.InvariantCulture), "hh:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out lastMessageTime) && lastMessageTime >= messageTime)
                    return;

                if (session.Cookies != null)
                    session.Cookies["LastMessageTime"] = messageTime.ToString("hh:mm:ss.fff", CultureInfo.InvariantCulture);
                session.Send(message);
            }
            catch
            {
                // ignored
            }
        }
        /*
        private void HandleEvent(PokeStopListEvent evt)
        {
            _lastPokeStopList = evt;
        }

        private void HandleEvent(ProfileEvent evt)
        {
            _lastProfile = evt;
        }
        */
        private void HandleMessage(WebSocketSession session, string message)
        {
            switch (message)
            {
                case "PokemonList":
                    //await PokemonListTask.Execute(_session);
                    break;
                case "EggsList":
                    //await EggsListTask.Execute(_session);
                    break;
            }
        }

        private void HandleSession(WebSocketSession session)
        {
            session.Charset = Encoding.UTF8;

            foreach (var message in _messages)
            {
                SendMessage(session, message.Item2, message.Item1);
            }
           
        }

        /*
        public void Listen(IEvent evt, Session session)
        {
            dynamic eve = evt;

            try
            {
                HandleEvent(eve);
            }
            catch
            {
                // ignored
            }

            Broadcast(Serialize(eve));
        }
        */
        /*
        private string Serialize(dynamic evt)
        {
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };

            return JsonConvert.SerializeObject(evt, Formatting.None, jsonSerializerSettings);
        }*/

        public void SetMaxLogLevel(LogLevel logLevel)
        {
            _maxLogLevel = logLevel;
        }


        public void Write(string message, LogLevel level = LogLevel.Info, ConsoleColor color = ConsoleColor.Black)
        {
            if (level > _maxLogLevel)
                return;

            var strError = "ERROR";
            var strAttention = "ATTENTION";
            var strInfo = "INFO";
            var strPokestop = "POKESTOP";
            var strFarming = "FARMING";
            var strRecycling = "RECYCLING";
            var strPKMN = "PKMN";
            var strTransfered = "TRANSFERED";
            var strEvolved = "EVOLVED";
            var strBerry = "BERRY";
            var strEgg = "EGG";
            var strDebug = "DEBUG";
            var strUpdate = "UPDATE";

            if (_session != null)
            {
                strError = _session.Translation.GetTranslation(TranslationString.LogEntryError);
                strAttention = _session.Translation.GetTranslation(TranslationString.LogEntryAttention);
                strInfo = _session.Translation.GetTranslation(TranslationString.LogEntryInfo);
                strPokestop = _session.Translation.GetTranslation(TranslationString.LogEntryPokestop);
                strFarming = _session.Translation.GetTranslation(TranslationString.LogEntryFarming);
                strRecycling = _session.Translation.GetTranslation(TranslationString.LogEntryRecycling);
                strPKMN = _session.Translation.GetTranslation(TranslationString.LogEntryPKMN);
                strTransfered = _session.Translation.GetTranslation(TranslationString.LogEntryTransfered);
                strEvolved = _session.Translation.GetTranslation(TranslationString.LogEntryEvolved);
                strBerry = _session.Translation.GetTranslation(TranslationString.LogEntryBerry);
                strEgg = _session.Translation.GetTranslation(TranslationString.LogEntryEgg);
                strDebug = _session.Translation.GetTranslation(TranslationString.LogEntryDebug);
                strUpdate = _session.Translation.GetTranslation(TranslationString.LogEntryUpdate);
            }

            var messageTime = DateTime.Now;
            switch (level)
            {
                case LogLevel.Error:
                    Broadcast($"[{messageTime.ToString("HH:mm:ss")}] ({strError}) {message}", messageTime);
                    break;
                case LogLevel.Warning:
                    Broadcast($"[{messageTime.ToString("HH:mm:ss")}] ({strAttention}) {message}", messageTime);
                    break;
                case LogLevel.Info:
                    Broadcast($"[{messageTime.ToString("HH:mm:ss")}] ({strInfo}) {message}", messageTime);
                    break;
                case LogLevel.Pokestop:
                    Broadcast($"[{messageTime.ToString("HH:mm:ss")}] ({strPokestop}) {message}", messageTime);
                    break;
                case LogLevel.Farming:
                    Broadcast($"[{messageTime.ToString("HH:mm:ss")}] ({strFarming}) {message}", messageTime);
                    break;
                case LogLevel.Recycling:
                    Broadcast($"[{messageTime.ToString("HH:mm:ss")}] ({strRecycling}) {message}", messageTime);
                    break;
                case LogLevel.Caught:
                    Broadcast($"[{messageTime.ToString("HH:mm:ss")}] ({strPKMN}) {message}", messageTime);
                    break;
                case LogLevel.Transfer:
                    Broadcast($"[{messageTime.ToString("HH:mm:ss")}] ({strTransfered}) {message}", messageTime);
                    break;
                case LogLevel.Evolve:
                    Broadcast($"[{messageTime.ToString("HH:mm:ss")}] ({strEvolved}) {message}", messageTime);
                    break;
                case LogLevel.Berry:
                    Broadcast($"[{messageTime.ToString("HH:mm:ss")}] ({strBerry}) {message}", messageTime);
                    break;
                case LogLevel.Egg:
                    Broadcast($"[{messageTime.ToString("HH:mm:ss")}] ({strEgg}) {message}", messageTime);
                    break;
                case LogLevel.Debug:
                    Broadcast($"[{messageTime.ToString("HH:mm:ss")}] ({strDebug}) {message}", messageTime);
                    break;
                case LogLevel.Update:
                    Broadcast($"[{messageTime.ToString("HH:mm:ss")}] ({strUpdate}) {message}", messageTime);
                    break;
                default:
                    Broadcast($"[{messageTime.ToString("HH:mm:ss")}] ({strError}) {message}", messageTime);
                    break;
            }
        }

        public void SetSession(ISession session)
        {
            _session = session;
        }
    }
}