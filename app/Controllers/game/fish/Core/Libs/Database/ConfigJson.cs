using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using System.Text;

namespace Database
{
    public class ConfigJson
    {
        static ConfigJson()
        {
            init();
        }

        public static Action OnConfigChange;
        private static long configHash = -1;
        private static FileSystemWatcher watcher;

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private static void init()
        {
            // Create a new FileSystemWatcher and set its properties.
            watcher = new FileSystemWatcher();
            watcher.Path = "./";

            // Watch for changes in LastAccess and LastWrite times, and
            // the renaming of files or directories.
            watcher.NotifyFilter = NotifyFilters.LastWrite;

            // Only watch text files.
            watcher.Filter = "config.json";

            // Add event handlers.
            watcher.Changed += OnChanged;
            //watcher.Created += OnChanged;
            //watcher.Deleted += OnChanged;
            //watcher.Renamed += OnRenamed;

            // Begin watching.
            watcher.EnableRaisingEvents = true;

            reload();
        }

        // Define the event handlers.
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            //Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
            reload();
        }

        //private static void OnRenamed(object source, RenamedEventArgs e) =>
        //    // Specify what is done when a file is renamed.
        //    Console.WriteLine($"File: {e.OldFullPath} renamed to {e.FullPath}");

        public static void reload()
        {
            try
            {
                StringBuilder builder = new StringBuilder();
                using (var stream = new FileStream(@"./config.json", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (line.StartsWith("//"))
                                continue;

                            builder.Append(line);
                            builder.Append(Environment.NewLine);
                        }
                    }
                }
                var json = builder.ToString().Trim();
                if (string.IsNullOrEmpty(json))
                {
                    Logger.Info("Config from file is empty");
                }
                else
                {
                    var newHash = json.GetHashCode();
                    if (configHash != newHash)
                    {
                        Config = JSON.Parse(json);
                        configHash = newHash;
                        Logger.Info("Loaded new config");
                        try
                        {
                            if (OnConfigChange != null) OnConfigChange();
                        }
                        catch (Exception ex2)
                        {
                            Logger.Error("Fail to invoke OnConfigChange: " + ex2.ToString());
                        }
                    }
                    else
                    {
                        Logger.Info("Config does not change");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Fail to load config json: " + ex.ToString());
            }
        }
        public static JSONNode Config { get; private set; }
    }
}
