﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Helios.Concurrency;
using WhatNow.Contracts;

namespace WhatNow.Essentials
{
    public class TaskList : ITaskList
    {
        readonly CancellationTokenSource cancellationTokenSource;
        readonly List<Task> tasks;

        readonly DedicatedThreadPool pool;
        readonly DedicatedThreadPoolTaskScheduler scheduler;
        readonly TaskFactory taskFactory;

        public int TasksCount
            => tasks.Count;

        public TaskList(int maxDegreeOfParallelism)
        {
            pool = new DedicatedThreadPool(new DedicatedThreadPoolSettings(maxDegreeOfParallelism));
            scheduler = new DedicatedThreadPoolTaskScheduler(pool);
            taskFactory = new TaskFactory(scheduler);

            cancellationTokenSource = new CancellationTokenSource();
            tasks = new List<Task>();
        }

        public TaskList(TaskFactory taskFactory)
        {
            pool = null;
            scheduler = null;
            this.taskFactory = taskFactory;

            cancellationTokenSource = new CancellationTokenSource();
            tasks = new List<Task>();
        }

        public ITaskList With(Action action)
        {
            tasks.Add(taskFactory.StartNew(action, cancellationTokenSource.Token));
            return this;
        }

        public ITaskList With(IEnumerable<Action> actions)
        {
            foreach (var action in actions)
                tasks.Add(taskFactory.StartNew(action, cancellationTokenSource.Token));

            return this;
        }

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
            pool?.Dispose();
        }

        public void WaitAllFinished()
            => Task.WaitAll(tasks.ToArray());

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            pool?.Dispose();
        }
    }
}
