﻿using PointBlank.Core;
using PointBlank.Core.Managers;
using PointBlank.Core.Managers.Events;
using PointBlank.Core.Managers.Server;
using PointBlank.Core.Network;
using PointBlank.Core.Xml;
using System;
using System.Reflection;
using PointBlank.Auth.Data.Configs;
using PointBlank.Auth.Data.Sync;
using PointBlank.Auth.Data.Xml;
using System.Net;

namespace PointBlank.Auth
{
    public class Programm
    {
        private static void Main(string[] args)
        {
            string Date = ComDiv.GetLinkerTime(Assembly.GetExecutingAssembly(), null).ToString("dd/MM/yyyy HH:mm");
            Console.Title = "Point Blank - Auth";
            Logger.StartedFor = "Auth";
            Logger.checkDirectorys();
            Console.Clear();
            Logger.title("________________________________________________________________________________");
            Logger.title("                                                                               ");
            Logger.title("                                                                               ");
            Logger.title("                                POINT BLANK AUTH                               ");
            Logger.title("                                                                               ");
            Logger.title("                                                                               ");
            Logger.title("_______________________________ " + Date + " _______________________________");
            AuthConfig.Load();
            ServerConfigSyncer.GenerateConfig(AuthConfig.configId);
            EventLoader.LoadAll();
            BasicInventoryXml.Load();
            CafeInventoryXml.Load();
            ServersXml.Load();
            ChannelsXml.Load(AuthConfig.serverId);
            MissionCardXml.LoadBasicCards(2);
            MapsXml.Load();
            ShopManager.Load(1);
            ShopManager.Load(2);
            RankXml.Load();
            RankXml.LoadAwards();
            CouponEffectManager.LoadCouponFlags();
            QuickStartXml.Load();
            ICafeManager.GetList();
            MissionsXml.Load();
            AuthSync.Start();
            bool started = AuthManager.Start();
            Logger.info("Text Encode: " + Config.EncodeText.EncodingName);
            Logger.info("Mode: " + (AuthConfig.isTestMode ? "Test" : "Public"));
            Logger.debug(StartSuccess());
            if (started)
            {
                Auth.Update();
            }

            while (true)
            {
                string Read = Console.ReadLine();
                if (Read.StartsWith("reload_event"))
                {
                    string Result = "";
                    try
                    {
                        EventLoader.ReloadAll();
                        Result = "Reloaded Event Success.";
                    }
                    catch
                    {
                        Result = "Command Error.";
                    }
                    Logger.console(Result);
                    Logger.LogConsole(Read, Result);
                }
            }
        }

        private static string StartSuccess()
        {
            if (Logger.erro)
            {
                return "Startup failed.";
            }
            return "Active Server. (" + DateTime.Now.ToString("dd/MM/yy HH:mm:ss") + ")";
        }
    }
}