﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WhatNow.Contracts;

namespace WhatNow.Essentials
{
    public class ActionDispatcher : IActionDispatcher
    {
        readonly IActionPipe[] pipes;
        readonly CancellationTokenSource cancellationTokenSource;
        readonly Dictionary<IActionPipe, Task> tasks;

        public ActionDispatcher(IEnumerable<IActionPipe> actionPipes)
        {
            pipes = actionPipes.ToArray();
            cancellationTokenSource = new CancellationTokenSource();
            tasks = new Dictionary<IActionPipe, Task>(pipes.Length);
        }

        public void DoEvents()
        {
            foreach (var pipe in pipes)
            {
                if (pipe.BreakRequested)
                    continue;

                if (tasks.ContainsKey(pipe) && !tasks[pipe].IsCompleted)
                    continue;

                if (pipe.TryGetNextTask(cancellationTokenSource.Token, out Task task))
                {
                    tasks[pipe].Dispose();
                    tasks[pipe] = task;
                }
            }
        }

        public bool IsFinished
            => pipes.All(p => p.Finished || p.BreakRequested);

        public bool EndedByBreak
            => pipes.Any(p => p.BreakRequested);

        public IEnumerable<BreakRequestReason> GetBreakReasons()
            => pipes.SelectMany(p => p.BreakReasons);

        public IEnumerable<(IActionPipe, ProcessingStatistics)> ProcessingStats
            => pipes.SelectMany(p => p.ProcessingStats.Select(s => (p, s)));

        public void Dispose()
        {
            cancellationTokenSource.Dispose();
            tasks.Values.ToList().ForEach(t => t.Dispose());
        }
    }
}