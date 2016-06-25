using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistantStorage
{
    internal class AsyncScheduler
    {
        private readonly List<Task> _taskQue;

        public AsyncScheduler()
        {
            _taskQue = new List<Task>();
        }

        public Task<T> AddTask<T>(Func<T> action) => AddTask(new Task<T>(action));

        public Task AddTask(Action action) => AddTask(new Task(action));

        public Task AddTask(Task newTask)
        {
            if (_taskQue.Count == 0)
            {
                _taskQue.Add(newTask);
                newTask.Start();
            }
            else
            {
                _taskQue.Add(newTask);
                _taskQue[_taskQue.Count - 2].ContinueWith((t) => newTask.Start());
            }

            return newTask;
        }

        public Task<T> AddTask<T>(Task<T> newTask)
        {
            if (_taskQue.Count == 0)
            {
                _taskQue.Add(newTask);
                newTask.Start();
            }
            else
            {
                _taskQue.Add(newTask);
                _taskQue[_taskQue.Count - 2].ContinueWith((t) => newTask.Start());
            }

            return newTask;
        }
    }
}
