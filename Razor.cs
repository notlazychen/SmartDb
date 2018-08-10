﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartDb
{
    public class Razor :IDisposable
    {
        private string _connstr;
        private IDbConnection _conn;
        /// <summary>
        /// 合并写入最大行数
        /// </summary>
        public int MaxLines { get; }
        //private SmartDbBus _bus = new SmartDbBus();
        private Stopwatch _watch = new Stopwatch();

        public delegate void SqlCommandExecutedDelegate(int exeLines, long exeMs, int remainLines);
        public event SqlCommandExecutedDelegate SqlCommandExecuted;
        /// <summary>
        /// 抓住此事件时请宕机!
        /// </summary>
        public event EventHandler<Exception> UnHandledSqlCommandExecuteException;

        public Razor(string connStr, int maxlines = 100)
        {
            _connstr = connStr;
            _conn = new MySqlConnection(connStr);
            _conn.Open();
            MaxLines = maxlines;
        }

        private bool Execute()
        {
            int lines = 0;
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i< MaxLines; i++)
            {
                if (SmartDbBus.SqlQueue.TryDequeue(out var sql))
                {
                    sb.Append(sql.EndsWith(";") ? sql : sql + ";");
                    lines++;
                }
                else
                {
                    break;
                }
            }

            //执行操作
            if (lines > 0)
            {
                _watch.Reset();
                _watch.Start();

                IDbTransaction trx = null;

                bool isopend = _conn.State == ConnectionState.Open;
                int retrytimes = 0;//重试十次
                do
                {
                    try
                    {
                        if (!isopend)
                        {
                            _conn.Open(); //数据库连接打开
                        }
                        trx = _conn.BeginTransaction();
                        isopend = true;
                    }
                    catch (MySqlException ex)
                    {
                        //if (ex.Message.Contains("Fatal error encountered during command execution."))
                        //{
                        //}
                        _conn = new MySqlConnection(_connstr);
                        retrytimes++;
                        Thread.Sleep(500 * retrytimes);
                    }
                } while (!isopend && retrytimes <= 10);
                
                if(trx == null)
                {
                    var ex = new SmartDbExecuteException(sb.ToString(), "Can't create db transaction");
                    UnHandledSqlCommandExecuteException(this, ex);
                    throw ex;
                }

                try
                {
                    var cmd = _conn.CreateCommand();
                    cmd.Transaction = trx;
                    cmd.CommandText = sb.ToString();
                    cmd.ExecuteNonQuery();
                    trx.Commit();
                }
                catch (Exception ex)
                {
                    trx.Rollback();
                    UnHandledSqlCommandExecuteException?.Invoke(this, ex);
                }
                finally
                {
                    trx.Dispose();
                }
                if (isopend)
                {
                    _conn.Close();//数据库连接关闭
                }

                _watch.Stop();
                this.OnProcessing(lines, _watch.ElapsedMilliseconds);
                return true;
            }
            return false;
        }

        private CancellationTokenSource _loopToken;
        /// <summary>
        /// 开始执行数据库写入循环任务
        /// </summary>
        public void StartWork()
        {
            _loopToken = new CancellationTokenSource();
            Thread thread = new Thread(()=> 
            {
                while (!_loopToken.IsCancellationRequested)
                {
                    try
                    {
                        if (!Execute())
                        {
                            Thread.Sleep(1);
                        }
                    }
                    catch (Exception ex)
                    {
                        UnHandledSqlCommandExecuteException?.Invoke(this, ex);
                    }
                }
            });
            thread.Start();
        }

        /// <summary>
        /// 停止执行循环任务
        /// </summary>
        public void StopWork()
        {
            if(_loopToken != null)
            {
                _loopToken.Cancel();
            }
        }

        public SmartDbSet<T> CreateDbSet<T>(IEnumerable<T> set)
            where T: class, new()
        {
            var result = new SmartDbSet<T>(set);            
            return result;
        }

        public SmartDbSet<T> CreateDbSet<T>()
            where T : class, new()
        {
            return this.CreateDbSet(new List<T>());
        }

        public T CreateDbEntity<T>() where T : class, new()
        {
            return SmartDbEntityAgentFactory.Of<T>();
        }

        protected virtual void OnProcessing(int lines, long elapsedMilliseconds)
        {
            SqlCommandExecuted?.Invoke(lines, elapsedMilliseconds, SmartDbBus.SqlQueue.Count);
        }

        public void Dispose()
        {
            this.StopWork();
        }
    }

}