using System;
using CoreChatApi.Repos;

namespace CoreChatApi.Logger
{
    public class SqlLogger
    {
        public DatabaseRepo _databaseRepo { get; set; }

        public SqlLogger(string dbConnectionString)
        {
            _databaseRepo = new DatabaseRepo(dbConnectionString);
            CreateLoggerTable();
        }

        public async void LogError(Exception exception, string message)
        {
            var logSql = @$"USE [CoreChat]
                INSERT INTO [dbo].[logger](
                            [level],
                            [message],
                            [exception],
                            [datetime]
                            )
                        VALUES(
                            'error',
                            '{message}',
                            '{exception.ToString()}',
                            GETDATE()
                            )";

            await _databaseRepo.ExecuteSQL(logSql);
        }

        public async void CreateLoggerTable()
        {
            var createLogTableSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='logger' AND xtype='U')
                CREATE TABLE logger (
					id int NOT NULL IDENTITY,
                    level TEXT NOT NULL,
                    message TEXT NOT NULL,
                    exception TEXT NOT NULL,
                    datetime DATETIME NOT NULL
                )";

            await _databaseRepo.ExecuteSQL(createLogTableSql);
        }
    }
}