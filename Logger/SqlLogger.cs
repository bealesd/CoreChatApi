using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using CoreChatApi.Repos;
using CoreChatApi.Dtos;

namespace CoreChatApi.Logger
{
    public class SqlLogger
    {
        private readonly string table = "logger";
        public DatabaseRepo _databaseRepo { get; set; }

        public SqlLogger(string dbConnectionString)
        {
            _databaseRepo = new DatabaseRepo(dbConnectionString);
            CreateLoggerTable();
        }

        internal async Task<bool> LogMessage(string message, string level)
        {
            var logSql = @$"USE [CoreChat]
                INSERT INTO [dbo].[{table}](
                            [level],
                            [message],
                            [datetime]
                            )
                        VALUES(
                            '{level}',
                            '{message}',
                            GETDATE()
                            )";

            return await _databaseRepo.ExecuteSQL(logSql);
        }

        internal async Task<LogDTO> GetLastLog()
        {
            var getLastRowSql = @$"
                    SELECT TOP(1) *   
                    FROM [dbo].[{table}]   
                    ORDER BY datetime DESC";
            var logs = await _databaseRepo.QuerySQL<LogDTO>(getLastRowSql);
            return logs.FirstOrDefault();
        }

        internal async Task<IEnumerable<LogDTO>> GetLogs()
        {
            var getLastTenRowSql = @$"
                    SELECT TOP(100) *   
                    FROM [dbo].[{table}]   
                    ORDER BY datetime DESC";

            return await _databaseRepo.QuerySQL<LogDTO>(getLastTenRowSql);  
        }

        internal async void CreateLoggerTable()
        {
            var createLogTableSql = @$"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{table}' AND xtype='U')
                CREATE TABLE {table} (
					id int NOT NULL IDENTITY,
                    level TEXT NOT NULL,
                    message TEXT NOT NULL,
                    datetime DATETIME NOT NULL
                )";

            await _databaseRepo.ExecuteSQL(createLogTableSql);
        }
    }
}