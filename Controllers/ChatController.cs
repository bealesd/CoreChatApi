﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data.OleDb;

namespace CoreChatApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ChatController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<ChatController> _logger;

        private readonly string connectionString;

        public ChatController(IConfiguration config, ILogger<ChatController> logger)
        {
            _config = config;
            _logger = logger;
            connectionString = _config.GetConnectionString("oleDb");

            CreateTableIfRequired();
        }

        [HttpGet]
        [ActionName("GetChats")]
        [Produces("application/json")]
        public async Task<IActionResult> GetChats()
        {
            var isConnectionInvalid = !await TestConnection();
            if (isConnectionInvalid)
            {
                Response.StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError;
                return Content("Failed to connect to database");
            }

            var getLastTenRowSql = @"
                    SELECT TOP(100) *   
                    FROM [dbo].[chat]   
                    ORDER BY datetime DESC";

            var chats = await QuerySQL<ChatDTO>(getLastTenRowSql);
            return Ok(chats);
        }

        [HttpGet("{id}")]
        [ActionName("GetChatsAfterId")]
        [Produces("application/json")]
        public async Task<IActionResult> GetChatsAfterId(int id)
        {
            var isConnectionInvalid = !await TestConnection();
            if (isConnectionInvalid)
            {
                Response.StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError;
                return Content("Failed to connect to database");
            }

            var getChatsAfterIdSql = $@"
                    SELECT *   
                    FROM [dbo].[chat]   
                    WHERE id > {id}";

            var chats = await QuerySQL<ChatDTO>(getChatsAfterIdSql);
            return Ok(chats);
        }

        [HttpPost]
        [ActionName("AddChat")]
        public async Task<IActionResult> AddChat(ChatDTO chat)
        {
            var isConnectionInvalid = !await TestConnection();
            if (isConnectionInvalid)
            {
                Response.StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError;
                return Content("Failed to connect to database");
            }

            var chatSql = @$"USE [CoreChat]
                INSERT INTO [dbo].[chat](
                            [name],
                            [message],
                            [datetime]
                            )
                        VALUES(
                            '{chat.Name}',
                            '{chat.Message}',
                            GETDATE()
                            )";
            var isSqlInvalid = !await ExecuteSQL(chatSql);
            if (isSqlInvalid)
            {
                Response.StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest;
                return Content("Failed to execute sql");
            }

            Response.StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status200OK;
            return Content("Added new message");
        }

        private async Task<bool> ExecuteSQL(string sql)
        {
            try
            {
                using (OleDbConnection con = new OleDbConnection(connectionString))
                {
                    await con.ExecuteAsync(sql);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<IEnumerable<T>> QuerySQL<T>(string sql)
        {
            try
            {
                using (OleDbConnection con = new OleDbConnection(connectionString))
                {
                    return await con.QueryAsync<T>(sql);
                }
            }
            catch
            {
                return new List<T>();
            }
        }

        private async void CreateTableIfRequired()
        {
            var isConnectionInvalid = !await TestConnection();
            if (isConnectionInvalid)
            {
                return;
            }

            var createChatTableSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='chat' AND xtype='U')
                CREATE TABLE chat (
					id int NOT NULL IDENTITY,
                    name TEXT NOT NULL,
                    message TEXT NOT NULL,
                    datetime DATETIME NOT NULL
                )";
            var isSqlInvalid = !await ExecuteSQL(createChatTableSql);
            if (isSqlInvalid)
            {
                return;
            }
        }

        private async Task<bool> TestConnection()
        {
            try
            {
                using (var con = new OleDbConnection(connectionString))
                {
                    await con.OpenAsync();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }

    public class ChatDTO
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string Message { get; set; }
        public DateTime? DateTime { get; set; }
    }
}
