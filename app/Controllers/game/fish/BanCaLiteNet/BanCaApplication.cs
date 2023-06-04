using BanCa.Libs;
using System;
using System.Threading;
using BanCa.Redis;
using System.Collections.Generic;
using BanCa.Sql;
using Nancy.Hosting.Self;
using MySqlProcess.Genneral;
using System.Text;
using Microsoft.Diagnostics.Runtime;
using System.Linq;
using System.Net.NetworkInformation;
using SimpleJSON;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;
using Database;

namespace BanCa
{
    class BanCaApplication : BanCaLib
    {
        private const string Version = "1.0.0";
        private static NancyHost Host;
        private static List<int> servers = new List<int>() { 8977 };
        //private static List<int> servers = new List<int>() { 8880 };
        private static List<int> wsServers = new List<int>() { 2083 }; // use ws
        //private static List<int> wsServers = new List<int>() { 0, 0 }; // no ws

        private const int MF_BYCOMMAND = 0x00000000;
        public const int SC_CLOSE = 0xF060;

        
        //static int count = 0;
        private static readonly AutoResetEvent _closing = new AutoResetEvent(false);

        static void Main(string[] args)
        {
            Thread.CurrentThread.Name = "main";
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Logger._setLogger(new PlatformLogger());

            Logger.Info("Current version " + Version);

            TaskRunner.RunOnPool(async () => await createServer());

            Console.CancelKeyPress += new ConsoleCancelEventHandler(OnExit);
            _closing.WaitOne();
        }

        protected static void OnExit(object sender, ConsoleCancelEventArgs args)
        {
            Console.WriteLine("Exit");
            _closing.Set();
        }

        private static void doPing(string address)
        {
            Thread t = new Thread(() =>
            {
                while (true)
                {
                    Ping pinger = null;
                    try
                    {
                        pinger = new Ping();
                        PingReply reply = pinger.Send(address);
                        var pingable = reply.Status == IPStatus.Success;
                        if (pingable)
                        {
                            Logger.Info("Ping ok: " + reply.RoundtripTime);
                        }
                        else
                        {
                            Logger.Info("Ping fail: " + reply.Status);
                        }
                    }
                    catch (PingException ex)
                    {
                        Logger.Info("Ping error: " + ex.ToString());
                    }
                    finally
                    {
                        if (pinger != null)
                        {
                            pinger.Dispose();
                        }
                    }

                    Thread.Sleep(1000);
                }
            });
            t.Start();
            t.Join();
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Error("Error: " + e.ToString());
            Logger.Error("Detail: " + e.ExceptionObject.ToString());
        }

        private static void startService()
        {
            var hostConfigs = new HostConfiguration();
            // //hostConfigs.UnhandledExceptionCallback += (ex) =>
            // //{
            // //    Logger.Error("Webservice error: " + ex.ToString());
            // //};
            string port = ConfigJson.Config["webservice-port"];
            hostConfigs.UrlReservations.CreateAutomatically = true;
            var host = new NancyHost(hostConfigs, new Uri("http://localhost:" + port));
            host.Start();
            Logger.Info("Service running on http://localhost:" + port);
            Host = host;
        }

        private static void stopService()
        {
             if (Host != null)
             {
                 Host.Stop();
                 Host.Dispose();
                 Host = null;
                 Logger.Info("Service stop running");
             }
        }

      
        private static async Task createServer()
        {
            var process = Process.GetCurrentProcess();
            try
            {
                process.PriorityClass = ProcessPriorityClass.AboveNormal;
            }
            catch
            {
                Logger.Info("Cannot change process priority");
            }

            

            List<BanCaServer> bancaServers = BanCaLib.bancaServerInstances;
            bancaServers.Clear();
            try
            {
                SqlLogger.InitDb();
                RedisManager._init();
                RedisManager.ServerList = servers;
                RedisManager.WsServerList = wsServers;
                
                RedisManager.ClearPlayerServer();
                await RedisManager.LoadConfig(true);
                await RedisManager.LoadLobbyConfig(true);
                await RedisManager.LoadBotConfig(true);
                await RedisManager.LoadTableId();

                
                RedisManager.LoadBlackList();
                FundManager.LoadFromRedis();
                FundManager.StartFakeFlushAll();

                startService();
                Logger.Info("Starting server");
                for (int i = 0, n = servers.Count; i < n; i++) // fresh start
                {
                    await RedisManager.ClearLogBlind(servers[i]);
                    RedisManager.LogServer(servers[i], 0, 0, 0);
                }

                for (int i = 0, n = servers.Count; i < n; i++)
                {
                    var server = new BanCaServer(servers[i], wsServers[i], false);
                    bancaServers.Add(server);
                    if (n == 1 || i == 1)
                    {
                        server.AllowSolo = true;
                        Logger.Info("Allow solo on server " + servers[i]);
                    }
                    if (n == 1 || i == 0)
                    {
                        server.StartBot();
                        Logger.Info("Start bot on server " + servers[i]);
                    }
                }
                Logger.Info("Server started");
            }
            catch (Exception ex)
            {
                Logger.Error("Error creating server: " + ex.ToString());
                // if (Host != null)
                // {
                //     Host.Dispose();
                //     Host = null;
                // }
                try
                {
                    foreach (var item in bancaServers)
                    {
                        item.Dispose();
                    }
                }
                catch { }
                bancaServers.Clear();
                Logger.Info("Retry in 5s...");
                Thread.Sleep(5000);
                TaskRunner.RunOnPool(async () => await createServer());
                return;
            }

            System.IO.File.WriteAllText("./running", DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            int state = 0;
            // health checking server
            Thread t = new Thread(() =>
            {
                bool terminating = false;
                var sub = RedisManager.GetSubscriber();
                sub.Subscribe("bc_cmd", (channel, message) => // beware this run on another thread
                {
                    if (!string.IsNullOrEmpty(message))
                    {
                        try
                        {
                            Logger.Info("Get published message: " + channel + ": " + message);
                            var tokens = ((string)message).Split(' ');
                            if (tokens.Length == 0)
                                return;

                            switch (tokens[0])
                            {
                                case "reloadConfig":
                                    Logger.Info("Reloading config");
                                    TaskRunner.RunOnPool(async () =>
                                    {
                                        try
                                        {
                                            await RedisManager.LoadConfig(true);
                                            Logger.Info("Reload config done");
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Error("Reload config fail: " + ex.ToString());
                                        }
                                    });
                                    break;
                                case "reloadEvent":
                                    Logger.Info("Reloading event");
                                    TaskRunner.RunOnPool(async () =>
                                    {
                                        try
                                        {
                                            await MySqlEvent.Reload();
                                            Logger.Info("Reload event done");
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Error("Reload event fail: " + ex.ToString());
                                        }
                                    });
                                    break;
                                case "resetConfig":
                                    Logger.Info("Resetting config");
                                    try
                                    {
                                        RedisManager.ResetConfig();
                                        Logger.Info("Reset config done");
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("Reset config fail: " + ex.ToString());
                                    }
                                    break;
                                case "getBestServer":
                                    if (tokens.Length > 1)
                                    {
                                        try
                                        {
                                            var blindIndex = int.Parse(tokens[1]);
                                            int bestServer = RedisManager.GetBestFitServer(blindIndex);
                                            Logger.Info("Best server is: " + bestServer);
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Error("Get best server fail: " + ex.ToString());
                                        }
                                    }
                                    else
                                    {
                                        Logger.Info("Missing blind index");
                                    }
                                    break;
                                case "terminate":
                                    Logger.Info("Terminating server...");
                                    terminating = true;
                                    try
                                    {
                                        for (int i = 0, n = bancaServers.Count; i < n; i++)
                                        {
                                            var server = bancaServers[i];
                                            server.TaskRun.QueueAction(() =>
                                            {
                                                try
                                                {
                                                    server.RemoveAllPlayers(); // remove player so their cash update to db
                                                    server.Stop();
                                                }
                                                catch (Exception ex)
                                                {
                                                    Logger.Info("Fail to remove all player:\n" + ex.ToString());
                                                }
                                            });
                                        }
                                        bancaServers.Clear();
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Info("Fail to remove all player 2:\n" + ex.ToString());
                                    }
                                    Logger.Info("Terminate server done");

                                    try
                                    {
                                        RedisManager.SaveBlackList();
                                        FundManager.SaveToRedis();
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Info("Fail to save to redis:\n" + ex.ToString());
                                    }

                                    // if (Host != null)
                                    // {
                                    //     try
                                    //     {
                                    //         Host.Dispose();
                                    //         Host = null;
                                    //     }
                                    //     catch (Exception ex)
                                    //     {
                                    //         Logger.Info("Fail to stop host:\n" + ex.ToString());
                                    //     }
                                    // }

                                    Logger.Info("Bye!");
                                    Thread.Sleep(5000);

                                    try
                                    {
                                        System.IO.File.Delete("./running");
                                    }
                                    catch { }
                                    if (ConfigJson.Config.HasKey("quit-cmd"))
                                    {
                                        try
                                        {
                                            var cmd = ConfigJson.Config["quit-cmd"].Value;
                                            var arg = ConfigJson.Config["quit-arg"].Value;
                                            Logger.Info(string.Format("Execute quit command: {0}, with arg: {1}", cmd, arg));
                                            if (string.IsNullOrEmpty(arg))
                                                System.Diagnostics.Process.Start(cmd);
                                            else
                                                System.Diagnostics.Process.Start(cmd, arg);
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Error("Fail to execute quit command: " + ex.ToString());
                                        }
                                    }
                                    else
                                        Environment.Exit(0);
                                    break;
                                case "reloadCash":
                                    if (tokens.Length > 2)
                                    {
                                        try
                                        {
                                            var userId = long.Parse(tokens[1]);
                                            var msg = SimpleJSON.JSON.LoadFromCompressedBase64(tokens[2]);
                                            //Logger.Info("Pushing message to " + userId + ": " + msg.ToString());
                                            for (int i = 0, n = bancaServers.Count; i < n; i++)
                                            {
                                                try
                                                {
                                                    var server = bancaServers[i];
                                                    server.TaskRun.QueueAction(() =>
                                                    {
                                                        server.ReloadCash(userId, msg);
                                                    });
                                                }
                                                catch (Exception ex)
                                                {
                                                    Logger.Info("Fail to reloadCash:\n" + ex.ToString());
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Info("Fail to reloadCash, invalid parameters:\n" + ex.ToString());
                                        }
                                    }
                                    break;
                                case "kick":
                                    if (tokens.Length > 1)
                                    {
                                        try
                                        {
                                            var userId = long.Parse(tokens[1]);
                                            for (int i = 0, n = bancaServers.Count; i < n; i++)
                                            {
                                                try
                                                {
                                                    var server = bancaServers[i];
                                                    server.TaskRun.QueueAction(() =>
                                                    {
                                                        server.Kick(userId);
                                                    });
                                                }
                                                catch (Exception ex)
                                                {
                                                    Logger.Info("Fail to kick:\n" + ex.ToString());
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Info("Fail to kick, invalid parameters:\n" + ex.ToString());
                                        }
                                    }
                                    break;
                                case "forceSetLeaderboard":
                                    TaskRunner.RunOnPool(async () =>
                                    {
                                        try
                                        {
                                            await RedisManager.SetTopWeek();
                                            await RedisManager.SetTopMonth();
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Error("Error in force set top: " + ex.ToString());
                                        }
                                    });
                                    break;
                                case "restartPort":
                                    if (tokens.Length > 1)
                                    {
                                        try
                                        {
                                            var port = long.Parse(tokens[1]);
                                            Logger.Info("Request restart port " + port);
                                            try
                                            {
                                                for (int i = 0, n = bancaServers.Count; i < n; i++)
                                                {
                                                    var server = bancaServers[i];
                                                    if (server.Port == port)
                                                    {
                                                        try
                                                        {
                                                            server.Stop();
                                                            server.Dispose();
                                                            server.Revive();
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Logger.Info("Fail to force revive: " + port + "\n" + ex.ToString());
                                                        }
                                                        break;
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Logger.Info("Fail to force revive 2: " + port + "\n" + ex.ToString());
                                            }
                                            Logger.Info("Try restart port end " + port);
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Info("Fail to force revive, invalid parameters:\n" + ex.ToString());
                                        }
                                    }
                                    break;
                                case "threadStat":
                                    {
                                        System.Diagnostics.Process procces = System.Diagnostics.Process.GetCurrentProcess();
                                        System.Diagnostics.ProcessThreadCollection threadCollection = procces.Threads;

                                        var strBuilder = new StringBuilder();
                                        strBuilder.Append("State: ");
                                        strBuilder.Append(state);
                                        strBuilder.Append(Environment.NewLine);
                                        strBuilder.Append("Number of threads: ");
                                        strBuilder.Append(threadCollection.Count);
                                        strBuilder.Append(" - stats:\r\n");
                                        foreach (System.Diagnostics.ProcessThread proccessThread in threadCollection)
                                        {
                                            strBuilder.Append(string.Format("Id: {0}, State: {1}, Start: {2}, Total: {3}, User: {4}, Address: {5}, Wait: \"{6}\"\r\n",
                                                proccessThread.Id, proccessThread.ThreadState, proccessThread.StartTime,
                                                proccessThread.TotalProcessorTime, proccessThread.UserProcessorTime, proccessThread.StartAddress,
                                                proccessThread.ThreadState == System.Diagnostics.ThreadState.Wait ? proccessThread.WaitReason.ToString() : ""));
                                        }

                                        Logger.Info(strBuilder.ToString());
                                    }
                                    break;
                                case "threadTrace":
                                    {
                                        using (DataTarget target = DataTarget.AttachToProcess(System.Diagnostics.Process.GetCurrentProcess().Id, 5000, AttachFlag.Passive))
                                        {
                                            var strBuilder = new StringBuilder();
                                            strBuilder.Append("Thread traces: ");
                                            ClrRuntime runtime = target.ClrVersions.First().CreateRuntime();
                                            foreach (ClrAppDomain domain in runtime.AppDomains)
                                            {
                                                strBuilder.Append(string.Format("ID:      {0}\r\n", domain.Id));
                                                strBuilder.Append(string.Format("Name:    {0}\r\n", domain.Name));
                                                strBuilder.Append(string.Format("Address: {0}\r\n", domain.Address));
                                            }

                                            foreach (ClrThread thread in runtime.Threads)
                                            {
                                                if (!thread.IsAlive)
                                                    continue;

                                                strBuilder.Append(string.Format("Thread {0}:\r\n", thread.OSThreadId));

                                                foreach (ClrStackFrame frame in thread.StackTrace)
                                                    strBuilder.Append(string.Format("{0,12:X} {1,12:X} {2}\r\n", frame.StackPointer, frame.InstructionPointer, frame.ToString()));
                                            }

                                            Logger.Info(strBuilder.ToString());
                                        }
                                    }
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Fail to process cmd: " + message + "\n" + ex.ToString());
                        }
                    }
                });
                while (true)
                {
                    state = 0;
                    try
                    {
                        // TODO: check maintain
                        //TaskRunner.RunOnPool(async () => Config.IsMaintain = await RedisManager.IsMaintain());
                        SqlLogger.OnInterval();
                        FundManager.SaveToRedis();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error in main loop: " + ex.ToString());
                    }

                    for (int u = 0; u < 60; u++)// checking per minute (60s)
                    {
                        state = 1;
                        while (BanCaLib.PendingAlerts.Count > 0)
                        {
                            JSONNode alert = null;
                            if (BanCaLib.PendingAlerts.TryDequeue(out alert))
                            {
                                for (int i = 0, n = bancaServers.Count; i < n; i++)
                                {
                                    try
                                    {
                                        var _userId = alert["userId"].AsLong;
                                        var server = bancaServers[i];
                                        server.TaskRun.QueueAction(() =>
                                        {
                                            server.Alert(_userId, alert);
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Info("Fail to reloadCash:\n" + ex.ToString());
                                    }
                                }
                            }
                            else
                            {
                                BanCaLib.ClearPendingAlert();
                                Logger.Info("Fail to dequeue PendingAlerts");
                            }
                        }

                        while (BanCaLib.pendingPushAll.TryDequeue(out var p))
                        {
                            for (int i = 0, n = bancaServers.Count; i < n; i++)
                            {
                                var server = bancaServers[i];
                                server.pendingPushAll.Enqueue(p);
                            }
                        }
                        Thread.Sleep(1000);
                        state = 2;
                        if (u % 5 == 0) // push jackpot
                        {
                            FundManager.IncJackpotFake();
                            var data = FundManager.GetJackpotJson();
                            for (int i = 0, n = bancaServers.Count; i < n; i++)
                            {
                                var server = bancaServers[i];
                                server.TaskRun.QueueAction(() => { server.UpdateJackpot(data); });
                            }
                        }

                        state = 3;
                        if (Monitor.TryEnter(BanCaLib.kickLock, 1000))
                        {
                            try
                            {
                                foreach (var id in BanCaLib.clientIdToKicks)
                                {
                                    for (int i = 0, n = bancaServers.Count; i < n; i++)
                                    {
                                        var server = bancaServers[i];
                                        server.TaskRun.QueueAction(() => { server.Kick(id); });
                                    }
                                }
                                BanCaLib.clientIdToKicks.Clear();
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Error while clientIdToKicks: " + ex.ToString());
                            }

                            state = 4;
                            try
                            {
                                for (int i = 0, n = bancaServers.Count; i < n; i++)
                                {
                                    var server = bancaServers[i];

                                    var list = BanCaLib.kickOnPorts.ContainsKey(server.Port) ? BanCaLib.kickOnPorts[server.Port] : null;
                                    if (list != null)
                                    {
                                        foreach (var userId in list)
                                        {
                                            server.TaskRun.QueueAction(() => { server.Kick(userId); });
                                        }
                                        list.Clear();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Error while kickOnPorts: " + ex.ToString());
                            }
                            finally
                            {
                                Monitor.Exit(BanCaLib.kickLock);
                            }
                        }
                        else
                        {
                            Logger.Info("Fail to accquire kickLock");
                        }
                        state = 5;
                    }

                    if (!terminating)
                    {
                        try
                        {
                            state = 6;
                            for (int i = 0, n = bancaServers.Count; i < n; i++)
                            {
                                var server = bancaServers[i];
                                if (server.IsDead())
                                {
                                    Logger.Info("Found server dead: " + server.Port);
                                    server.Revive();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Error while reviving server: " + ex.ToString());
                        }
                    }
                }
            });
            t.Name = "ServerHealth";
            t.IsBackground = false;
            t.Start();
        }
    }
}
