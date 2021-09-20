using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using CoreChatApi.Dtos;
using CoreChatApi.Repos;
using CoreChatApi.Logger;

namespace CoreChatApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class LogController : ControllerBase
    {
        private readonly ILogger<LogController> _logger;
        private readonly string _dbConnectionString;
        private DatabaseRepo _databaseRepo { get; set; }
        public SqlLogger _myLogger { get; set; }

        public LogController(IConfiguration config, ILogger<LogController> logger)
        {
            _dbConnectionString = config.GetConnectionString("db");

            _logger = logger;
            _logger.LogInformation($"Connection string:\n\t{_dbConnectionString}");

            _myLogger = new SqlLogger(_dbConnectionString);

            _databaseRepo = new DatabaseRepo(_dbConnectionString, _myLogger);

            CreateLogTable();
        }

        [HttpGet]
        [ActionName("GetLogs")]
        [Produces("application/json")]
        public async Task<IActionResult> GetLogs()
        {
            var getLastTenRowSql = @"
                    SELECT TOP(100) *   
                    FROM [dbo].[logger]   
                    ORDER BY datetime DESC";

            var logs = (await _databaseRepo.QuerySQL<LogDTO>(getLastTenRowSql)).ToList();
            if (logs == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(logs);
        }

        [HttpPost]
        [ActionName("AddLog")]
        public async Task<IActionResult> AddLog(LogDTO log)
        {
            var chatSql = @$"USE [CoreChat]
                INSERT INTO [dbo].[logger](
                            [level],
                            [message],
                            [datetime]
                            )
                        VALUES(
                            '{log.Level}',
                            '{log.Message}',
                            GETDATE()
                            )";
            var isSqlInvalid = !await _databaseRepo.ExecuteSQL(chatSql);
            if (isSqlInvalid)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            var lastLog = await GetLastLog();
             if (lastLog == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(lastLog);
        }

          private async Task<LogDTO> GetLastLog()
        {
            var getLastRowSql = @"
                    SELECT TOP(1) *   
                    FROM [dbo].[logger]   
                    ORDER BY datetime DESC";
            var logs = await _databaseRepo.QuerySQL<LogDTO>(getLastRowSql);
            return logs.FirstOrDefault();
        }

        private async void CreateLogTable()
        {
            var createLogTableSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='logger' AND xtype='U')
                CREATE TABLE chat (
					id int NOT NULL IDENTITY,
                    level TEXT NOT NULL,
                    message TEXT NOT NULL,
                    datetime DATETIME NOT NULL
                )";
            await _databaseRepo.ExecuteSQL(createLogTableSql);
        }
    }
}
