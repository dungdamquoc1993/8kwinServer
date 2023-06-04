using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BanCa.Libs;
#if !UNITY_WEBGL
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
#endif


namespace BanCa.Libs
{
    public delegate bool ExecuteCondition();

    class ExecutionConditioner
    {
        public ExecuteCondition Condition;
        public Action Action;
        public long TimeOutAt;

        public ExecutionConditioner(ExecuteCondition Condition, Action Action, long TimeOutAt)
        {
            this.Condition = Condition;
            this.Action = Action;
            this.TimeOutAt = TimeOutAt;
        }
    }

    class TaskWithResult
    {
        internal object task;
        internal Action<object> result;

        internal void clear()
        {
            task = null;
            result = null;
        }
    }

    class ActionWithResult
    {
        internal object result;
        internal Action<object> action;

        internal void execute()
        {
            action(result);
        }

        internal void clear()
        {
            action = null;
            result = null;
        }
    }

    class IntervalTask : IDisposable
    {
        public long IntervalMs;
        public long NextProcessTimestamp;
        public Action TaskAction;
        public bool Enable = true;

        public void Dispose()
        {
            Enable = false;
        }
    }

    public class TaskRunner
    {
        public static long MaxExecutionTimeMs = 16;

        public static void RunOnPool(Action cb)
        {
#if UNITY_WEBGL
            try
            {
                cb();
            }
            catch (Exception ex)
            {
                Logger.Error("Error while RunOnPool: " + ex.ToString());
            }
#else
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    cb();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error while RunOnPool: " + ex.ToString());
                }
            });
#endif
        }

        public static void RunOnPool(Func<Task> cb)
        {
#if UNITY_WEBGL
            try
            {
                cb();
            }
            catch (Exception ex)
            {
                Logger.Error("Error while RunOnPool: " + ex.ToString());
            }
#else
            ThreadPool.QueueUserWorkItem(async o =>
            {
                try
                {
                    await cb();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error while RunOnPool: " + ex.ToString());
                }
            });
#endif
        }

        public static void RunOnThread(Action cb)
        {
            Thread t = new Thread(o =>
            {
                try
                {
                    cb();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error while RunOnThread: " + ex.ToString());
                }
            });
            t.Start();
        }

#if !UNITY_WEBGL
        public static void RunOnPoolAsync<T>(Task<T> task, Action<T> cb = null)
        {
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                try
                {
                    var t = await task;
                    cb?.Invoke(t);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error while RunOnPool: " + ex.ToString());
                }
            });
        }
        public delegate Task<object> TaskActionAsync();
        private StackTrace callingStackTrace;
#endif
        public const int TASK_LIMIT = 2048;
        public delegate object TaskAction();
        private bool overflow = false;

        private ConcurrentQueue<Action> tasks = new ConcurrentQueue<Action>();
        private ConcurrentQueue<ActionWithResult> taskResults = new ConcurrentQueue<ActionWithResult>();
        private List<ExecutionConditioner> conditionExecutioner = new List<ExecutionConditioner>();
        private List<IntervalTask> intervalTasks = new List<IntervalTask>();

        private SimplePool<TaskWithResult> taskWithResultPool = new SimplePool<TaskWithResult>(() => new TaskWithResult());
        private SimplePool<ActionWithResult> actionWithResultPool = new SimplePool<ActionWithResult>(() => new ActionWithResult());
        public TaskRunner(bool selfOperate = false)
        {
#if UNITY_WEBGL
            if (selfOperate)
                throw new Exception("WebGl does not support selfOperate");
#else
            if (selfOperate)
            {
                SelfOperate();
            }
            callingStackTrace = new StackTrace();
#endif
        }

        public System.Timers.Timer SetTimerInterval(long intervalMs, Action cb, bool queueCb = true)
        {
            var timer = new System.Timers.Timer(intervalMs);
            var processing = false;
            timer.Elapsed += new System.Timers.ElapsedEventHandler((s, a) =>
            {
                lock (timer)
                {
                    if (processing)
                    {
#if !UNITY_WEBGL
                        if (callingStackTrace != null)
                            Logger.Error("Taskrun SetInterval overload: " + callingStackTrace.ToString());
#endif
                        return;
                    }
                    processing = true;
                }
                try
                {
                    if (queueCb) QueueAction(cb); else cb();
                }
                catch (Exception ex)
                {
                    Logger.Error("Exception in wait SetInterval: " + ex.ToString());
#if !UNITY_WEBGL
                    if (callingStackTrace != null)
                        Logger.Error("Origin: " + callingStackTrace.ToString());
#endif
                }
                finally
                {
                    lock (timer)
                        processing = false;
                }
            });
            timer.Start();

            return timer;
        }

        public IDisposable SetInterval(long intervalMs, Action cb)
        {
            var task = new IntervalTask()
            {
                IntervalMs = intervalMs,
                NextProcessTimestamp = TimeUtil.TimeStamp + intervalMs,
                TaskAction = cb
            };
            lock (intervalTasks) intervalTasks.Add(task);
#if !UNITY_WEBGL
            if (operateThreadEvent != null)
            {
                operateThreadEvent.Set();
                setCount++;
            }
#endif
            return task;
        }

        public void QueueAction(TaskAction action, Action<object> cb)
        {
            var task = taskWithResultPool.Obtain();
            task.task = action;
            task.result = cb;
#if UNITY_WEBGL
            queueActionInternal(task);
#else
            ThreadPool.QueueUserWorkItem(queueActionInternal, task);
#endif
        }
        private void queueActionInternal(object o)
        {
            try
            {
                var task = (TaskWithResult)o;
                var action = (TaskAction)task.task;
                var result = action();
                var cb = task.result;

                var ar = actionWithResultPool.Obtain();
                ar.action = cb;
                ar.result = result;
                enqueueTaskResult(ar);

                task.clear();
                taskWithResultPool.Free(task);
            }
            catch (Exception ex)
            {
                Logger.Error("Exception in queue action: " + ex.ToString());
#if !UNITY_WEBGL
                if (callingStackTrace != null)
                    Logger.Error("Origin: " + callingStackTrace.ToString());
#endif
            }
        }

#if !UNITY_WEBGL
        public void QueueAction(long waitMilis, TaskAction action, Action<object> cb)
        {
            var timer = new System.Timers.Timer((double)waitMilis);
            timer.Elapsed += new System.Timers.ElapsedEventHandler((s, a) =>
            {
                try
                {
                    timer.Stop();
                    timer.Dispose();
                }
                catch { }

                try
                {
                    var result = action();
                    var ar = actionWithResultPool.Obtain();
                    ar.action = cb;
                    ar.result = result;
                    enqueueTaskResult(ar);
                }
                catch (Exception ex)
                {
                    Logger.Error("Exception in wait queue action: " + ex.ToString());
#if !UNITY_WEBGL
                    if (callingStackTrace != null)
                        Logger.Error("Origin: " + callingStackTrace.ToString());
#endif
                }
            });
            timer.Start();
        }
#endif

#if !UNITY_WEBGL
        public void QueueAction(TaskActionAsync action, Action<object> cb)
        {
            var task = taskWithResultPool.Obtain();
            task.task = action;
            task.result = cb;
            ThreadPool.QueueUserWorkItem(queueActionInternalAsync, task);
        }
        private async void queueActionInternalAsync(object o)
        {
            try
            {
                var task = (TaskWithResult)o;
                var action = (TaskActionAsync)task.task;
                var result = await action();
                var cb = task.result;

                var ar = actionWithResultPool.Obtain();
                ar.action = cb;
                ar.result = result;
                enqueueTaskResult(ar);

                task.clear();
                taskWithResultPool.Free(task);
            }
            catch (Exception ex)
            {
                Logger.Error("Exception in queue action: " + ex.ToString());
#if !UNITY_WEBGL
                if (callingStackTrace != null)
                    Logger.Error("Origin: " + callingStackTrace.ToString());
#endif
            }
        }

        public void QueueAction(long waitMilis, TaskActionAsync action, Action<object> cb)
        {
            var timer = new System.Timers.Timer((double)waitMilis);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(async (s, a) =>
            {
                try
                {
                    timer.Stop();
                    timer.Dispose();
                }
                catch { }

                try
                {
                    var result = await action();
                    var ar = actionWithResultPool.Obtain();
                    ar.action = cb;
                    ar.result = result;
                    enqueueTaskResult(ar);
                }
                catch (Exception ex)
                {
                    Logger.Error("Exception in wait queue action: " + ex.ToString());
#if !UNITY_WEBGL
                    if (callingStackTrace != null)
                        Logger.Error("Origin: " + callingStackTrace.ToString());
#endif
                }
            });
            timer.Start();
        }
#endif
        public void QueueAction(Action cb)
        {
            tasks.Enqueue(cb);
            if (tasks.Count > TASK_LIMIT)
            {
#if !UNITY_WEBGL
                if (callingStackTrace != null && !overflow)
                {
                    overflow = true;
                    Logger.Error("Taskrun overflow in QueueAction: " + callingStackTrace.ToString());
                    Logger.Error("Taskrun overflow from: " + (new StackTrace()).ToString());
                }
#endif
                while (tasks.Count > TASK_LIMIT)
                {
                    try
                    {
                        tasks.TryDequeue(out var task);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Exception in QueueAction: " + ex.ToString());
#if !UNITY_WEBGL
                        if (callingStackTrace != null)
                            Logger.Error("Origin: " + callingStackTrace.ToString());
#endif
                    }
                }
            }
#if !UNITY_WEBGL
            if (operateThreadEvent != null)
            {
                operateThreadEvent.Set();
                setCount++;
            }
#endif
        }
        private void enqueueTaskResult(ActionWithResult ar)
        {
            taskResults.Enqueue(ar);
            if (taskResults.Count > TASK_LIMIT)
            {
#if !UNITY_WEBGL
                if (callingStackTrace != null && !overflow)
                {
                    overflow = true;
                    Logger.Error("Taskrun overflow in enqueueTaskResult: " + callingStackTrace.ToString());
                    Logger.Error("Taskrun overflow from: " + (new StackTrace()).ToString());
                }
#endif
                while (taskResults.Count > TASK_LIMIT)
                {
                    try
                    {
                        taskResults.TryDequeue(out var task);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Exception in enqueueTaskResult: " + ex.ToString());
#if !UNITY_WEBGL
                        if (callingStackTrace != null)
                            Logger.Error("Origin: " + callingStackTrace.ToString());
#endif
                    }
                }
            }
#if !UNITY_WEBGL
            if (operateThreadEvent != null)
            {
                operateThreadEvent.Set();
                setCount++;
            }
#endif
        }

        public void QueueAction(Action<object> cb, object data)
        {
            var ar = actionWithResultPool.Obtain();
            ar.action = cb;
            ar.result = data;
            enqueueTaskResult(ar);
        }

        public System.Timers.Timer QueueAction(long waitMilis, Action cb)
        {
            var timer = new System.Timers.Timer((double)waitMilis);
            timer.Elapsed += new System.Timers.ElapsedEventHandler((s, a) =>
            {
                try
                {
                    timer.Stop();
                    timer.Dispose();
                    QueueAction(cb);
                }
                catch (Exception ex)
                {
                    Logger.Error("Exception in wait QueueAction: " + ex.ToString());
#if !UNITY_WEBGL
                    if (callingStackTrace != null)
                        Logger.Error("Origin: " + callingStackTrace.ToString());
#endif
                }
            });
            timer.Start();
            return timer;
        }

        public void ExecutionByCondition(ExecuteCondition condition, Action action, long timeOutAt = -1)
        {
            lock (conditionExecutioner)
                conditionExecutioner.Add(new ExecutionConditioner(condition, action, timeOutAt));
#if !UNITY_WEBGL
            if (operateThreadEvent != null)
            {
                operateThreadEvent.Set();
                setCount++;
            }
#endif
        }

        public virtual void Update() // run update on main thread as soon as possible to poll event
        {
            var frameTime = MaxExecutionTimeMs;
            var start = TimeUtil.TimeStamp;
            var count = 0;

            while (taskResults.Count > 0)
            {
                try
                {
                    if (taskResults.TryDequeue(out var task) && task != null)
                    {
                        task.execute();
                        task.clear();
                        actionWithResultPool.Free(task);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Exception in callback queue: " + ex.ToString());
#if !UNITY_WEBGL
                    if (callingStackTrace != null)
                        Logger.Error("Origin: " + callingStackTrace.ToString());
#endif
                }

                if (operateThread == null)
                {
                    count++;
                    var total = TimeUtil.TimeStamp - start;
                    if (total > frameTime)
                    {
                        Logger.Info(string.Format("Time queue (2) {0} > frame time {1}, processed {2}, remain {3}", total, frameTime, count, taskResults.Count));
                        break;
                    }
                }
            }

            lock (conditionExecutioner)
            {
                for (int i = 0, n = conditionExecutioner.Count; i < n; i++)
                {
                    var ce = conditionExecutioner[i];
                    if ((ce.TimeOutAt != -1 && ce.TimeOutAt < start) || ce.Condition())
                    {
                        QueueAction(ce.Action);
                        conditionExecutioner[i] = conditionExecutioner[n - 1];
                        conditionExecutioner.RemoveAt(n - 1);
                        n--;
                        i--;
                    }
                }
            }

            lock (intervalTasks)
            {
                for (int i = 0, n = intervalTasks.Count; i < n; i++)
                {
                    var it = intervalTasks[i];
                    if (it.Enable)
                    {
                        if (it.NextProcessTimestamp < start)
                        {
                            it.NextProcessTimestamp = start + it.IntervalMs;
                            QueueAction(it.TaskAction);
                        }
                    }
                    else
                    {
                        intervalTasks[i] = intervalTasks[n - 1];
                        intervalTasks.RemoveAt(n - 1);
                        i--;
                        n--;
                    }
                }
            }

            while (tasks.Count > 0)
            {
                try
                {
                    if (tasks.TryDequeue(out var task) && task != null)
                    {
                        task();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Exception in callback queue: " + ex.ToString());
#if !UNITY_WEBGL
                    if (callingStackTrace != null)
                        Logger.Error("Origin: " + callingStackTrace.ToString());
#endif
                }

                if (operateThread == null)
                {
                    count++;
                    var total = TimeUtil.TimeStamp - start;
                    if (total > frameTime)
                    {
                        Logger.Info(string.Format("Time queue {0} > frame time {1}, processed {2}, remain {3}", total, frameTime, count, tasks.Count));
                        break;
                    }
                }
            }
        }

#if !UNITY_WEBGL
        private Thread operateThread;
        private bool running = false;
        private AutoResetEvent operateThreadEvent;
        private volatile int setCount = 0;
        /// <summary>
        /// Start a thread by itself so no need to call Update() manually, beware all callback will run on this thread
        /// </summary>
        public void SelfOperate(int loopIntervalMs = 100)
        {
            if (!running)
            {
                if (operateThreadEvent != null) operateThreadEvent.Dispose();

                running = true;
                operateThreadEvent = new AutoResetEvent(false);
                operateThread = new Thread(() =>
                {
                    operateThread.Name = "TaskRun_" + operateThread.ManagedThreadId;
                    while (running)
                    {
                        Update();

                        if (tasks.Count > 0 || taskResults.Count > 0)
                        {
                            Thread.Sleep(loopIntervalMs);
                        }
                        else
                        {
                            var taskCount = 0;
                            lock (conditionExecutioner)
                            {
                                taskCount += conditionExecutioner.Count;
                            }
                            lock (intervalTasks)
                            {
                                taskCount += intervalTasks.Count;
                            }
                            if (taskCount > 0)
                            {
                                Thread.Sleep(loopIntervalMs);
                            }
                            else
                            {
                                setCount--;
                                if (setCount <= 0)
                                {
                                    setCount = 0;
                                    operateThreadEvent.WaitOne(); // no task
                                }
                                else
                                {
                                    Thread.Sleep(loopIntervalMs);
                                }
                            }
                        }
                    }

                    operateThread = null;
                });
                operateThread.IsBackground = true;
                operateThread.Start();
            }
        }
        public void CancelSelfOperate()
        {
            running = false;
        }
#endif
    }
}
