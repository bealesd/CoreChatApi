using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Dapper;
using CoreChatApi.Dtos;
using CoreChatApi.Repos;
using CoreChatApi.Logger;
using System.Collections;
using System.Text;

namespace CoreChatApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ChatGroupsController : ControllerBase
    {
        private readonly string table = "chatGroups";
        private readonly ILogger<ChatGroupsController> _logger;
        private readonly string _dbConnectionString;
        private DatabaseRepo _databaseRepo { get; set; }
        public SqlLogger _myLogger { get; set; }

        public ChatGroupsController(IConfiguration config, ILogger<ChatGroupsController> logger)
        {
            _dbConnectionString = config.GetConnectionString("db");

            _logger = logger;
            _logger.LogInformation($"Connection string:\n\t{_dbConnectionString}");

            _myLogger = new SqlLogger(_dbConnectionString);

            _databaseRepo = new DatabaseRepo(_dbConnectionString, _myLogger);

            CreateChatGroupsTable();
        }

        [Authorize]
        [HttpGet("{id}")]
        [ActionName("GetChatGroupsById")]
        [Produces("application/json")]
        public async Task<IActionResult> GetChatGroupsByUsernameId(int usernameId)
        {
            var getChatsGroupsBuUsernameIdSql = @$"
                    SELECT *   
                    FROM [dbo].[{table}]   
                    WHERE usernameId = @usernameId";

            var parameters = new DynamicParameters(new { usernameId = usernameId});
            var chatGroups = await _databaseRepo.QuerySQL<ChatGroupDTO>(getChatsGroupsBuUsernameIdSql, parameters);
            if (chatGroups == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(chatGroups);
        }

        [Authorize]
        [HttpPost]
        [ActionName("AddChatGroup")]
        public async Task<IActionResult> AddChatGroup(List<int> usernameIds)
        {
            var chatGroupsSql = new StringBuilder();
            chatGroupsSql.Append("USE [CoreChat]");
            foreach (var usernameId in usernameIds)
            {
                var chatGroupForUser = @$"
                INSERT INTO [dbo].[{table}](
                            [usernameID]
                            )
                        VALUES(
                            {usernameId}
                            )";
                chatGroupsSql.Append(chatGroupForUser);
            }

            var isSqlInvalid = !await _databaseRepo.ExecuteSQL(chatGroupsSql.ToString(), null);
            if (isSqlInvalid)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            var lastChat = await GetLastChatGroup();
             if (lastChat == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(lastChat);
        }

        private async Task<ChatGroupDTO> GetLastChatGroup()
        {
            var getLastRowSql = @$"
                    SELECT *   
                    FROM [dbo].[{table}]
                    WHERE id = ( SELECT MAX(id) FROM [dbo].[{table}])";
            var chats = await _databaseRepo.QuerySQL<ChatGroupDTO>(getLastRowSql);
            return chats.FirstOrDefault();
        }

        private async void CreateChatGroupsTable()
        {
            var createChatGroupTableSql = @$"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{table}' AND xtype='U')
                CREATE TABLE {table} (
					id INT NOT NULL IDENTITY,
                    usernameId INT NOT NULL
                )";
            await _databaseRepo.ExecuteSQL(createChatGroupTableSql);
        }

        //what chats should i see
        //1. i am user 'david'
        //2. i look up david in chat groups lookup table
        //3. i choose a chat group that has an id
        //4. i get all chats of this group by using id on chats table
    }
}
