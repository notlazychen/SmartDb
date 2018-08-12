using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SmartDb
{
    interface ISmartable
    {
        
    }

    public class SmartDbSet<T> : IEnumerable<T>, ISmartable
         where T : class, new()
    {
        protected readonly List<T> _data = new List<T>();

        public int Count => _data.Count;

        //public bool Contains(T item)
        //{
        //    return _data.Contains(item);
        //}

        /// <summary>
        /// 增
        /// </summary>
        public T Add(T item)
        {
            T agent = SmartDbEntityAgentFactory.Of(item);
            _data.Add(agent);
            //记录到案
            this.WriteToDb(agent, DbActionType.Insert);
            return agent;
        }

        /// <summary>
        /// 删
        /// </summary>
        public void Clear()
        {
            foreach(var item in _data)
            {
                //记录到案
                this.WriteToDb(item, DbActionType.Delete);
            }
            _data.Clear();
        }

        /// <summary>
        /// 删
        /// </summary>
        public bool Remove(T item)
        {
            bool result = _data.Remove(item);

            if (result)
            {
                //记录到案
                this.WriteToDb(item, DbActionType.Delete);
            }
            return result;
        }

        internal SmartDbSet(IEnumerable<T> data)
        {
            foreach(var item in data)
            {
                T agent = SmartDbEntityAgentFactory.Of(item);
                _data.Add(agent);
            }
        }

        private void WriteToDb(T item, DbActionType type)
        {
            SmartDbBus.PlanToWriteToDb(item, type);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }
    }
}
