using BanCa.Libs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Utils;

namespace Messaging
{
    public class PubSub
    {
        private static readonly object _lock = new object();
        private static Dictionary<string, List<Action<string, string>>> topicHandleMap = new Dictionary<string, List<Action<string, string>>>();
        private static Dictionary<Action<string, string>, string> handleTopicMap = new Dictionary<Action<string, string>, string>();

        private static TaskRunner runner;
        private static Thread messagingThread;
        private static volatile bool running = false;

        public static void Start()
        {
            if (!running)
            {
                running = true;
                runner = new TaskRunner();
                messagingThread = new Thread(() =>
                {
                    while (running)
                    {
                        update();
                        Thread.Sleep(100);
                    }

                    runner = null;
                    messagingThread = null;
                });
                messagingThread.IsBackground = true;
                messagingThread.Name = "Messaging_Thread";
                messagingThread.Start();
            }
        }

        public static void Stop()
        {
            if (running && messagingThread != null)
            {
                running = false;
            }
        }

        public static void Publish(string topic, string content, long delayMs)
        {
            runner.QueueAction(delayMs, () =>
            {
                lock (_lock)
                {
                    topicHandleMap.TryGetValue(topic, out var listHandles);
                    if (listHandles != null)
                    {
                        for (int i = 0, n = listHandles.Count; i < n; i++)
                        {
                            try
                            {
                                listHandles[i].Invoke(topic, content);
                            }
                            catch (Exception ex)
                            {
                                Logger.Info("Fail to pubsub handler, topic {0}, content {1}, ex {2}", topic, content, ex.ToString());
                            }
                        }
                    }
                }
            });
        }

        public static void Publish(string topic, string content)
        {
            runner.QueueAction(() =>
            {
                lock (_lock)
                {
                    topicHandleMap.TryGetValue(topic, out var listHandles);
                    if (listHandles != null)
                    {
                        for (int i = 0, n = listHandles.Count; i < n; i++)
                        {
                            try
                            {
                                listHandles[i].Invoke(topic, content);
                            }
                            catch(Exception ex)
                            {
                                Logger.Info("Fail to pubsub handler, topic {0}, content {1}, ex {2}", topic, content, ex.ToString());
                            }
                        }
                    }
                }
            });
        }

        public static void Subscribe(string topic, Action<string, string> handler)
        {
            lock (_lock)
            {
                topicHandleMap.TryGetValue(topic, out var listHandles);
                if (listHandles != null)
                {
                    listHandles.Add(handler);
                }
                else
                {
                    var list = new List<Action<string, string>>();
                    list.Add(handler);
                    topicHandleMap.Add(topic, list);
                }

                handleTopicMap[handler] = topic;
            }
        }

        public static void UnSubscribe(Action<string, string> handler)
        {
            lock (_lock)
            {
                if (handleTopicMap.ContainsKey(handler))
                {
                    string topic = handleTopicMap[handler];
                    handleTopicMap.Remove(handler);

                    topicHandleMap.TryGetValue(topic, out var listHandles);
                    if (listHandles != null)
                    {
                        listHandles.Remove(handler);
                    }
                }
            }
        }

        private static void update()
        {
            runner.Update();
        }
    }
}
