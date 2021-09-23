using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Dapper;
using CoreChatApi.Dtos;
using CoreChatApi.Repos;
using CoreChatApi.Logger;

namespace CoreChatApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ChatController : ControllerBase
    {
        private readonly string table = "chat";
        private readonly ILogger<ChatController> _logger;
        private readonly string _dbConnectionString;
        private DatabaseRepo _databaseRepo { get; set; }
        public SqlLogger _myLogger { get; set; }

        public ChatController(IConfiguration config, ILogger<ChatController> logger)
        {
            _dbConnectionString = config.GetConnectionString("db");

            _logger = logger;
            _logger.LogInformation($"Connection string:\n\t{_dbConnectionString}");

            _myLogger = new SqlLogger(_dbConnectionString);

            _databaseRepo = new DatabaseRepo(_dbConnectionString, _myLogger);

            CreateChatTable();
        }

        [HttpGet]
        [ActionName("GetChats")]
        [Produces("application/json")]
        public async Task<IActionResult> GetChats()
        {
            var getLastTenRowSql = @$"
                    SELECT TOP(100) *   
                    FROM [dbo].[{table}]   
                    ORDER BY datetime DESC";

            var chats = (await _databaseRepo.QuerySQL<ChatDTO>(getLastTenRowSql)).ToList();
            if (chats == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(chats);
        }

        [HttpGet("{id}")]
        [ActionName("GetChatsAfterId")]
        [Produces("application/json")]
        public async Task<IActionResult> GetChatsAfterId(int id)
        {
            var getChatsAfterIdSql = @$"
                    SELECT *   
                    FROM [dbo].[{table}]   
                    WHERE id > @id";

            var parameters = new DynamicParameters(new { id = id});
            var chats = await _databaseRepo.QuerySQL<ChatDTO>(getChatsAfterIdSql, parameters);
            if (chats == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(chats);
        }

        [HttpPost]
        [ActionName("AddChat")]
        public async Task<IActionResult> AddChat(ChatDTO chat)
        {
            var chatSql = @$"USE [CoreChat]
                INSERT INTO [dbo].[{table}](
                            [name],
                            [message],
                            [datetime]
                            )
                        VALUES(
                            '@name',
                            '@message',
                            GETDATE()
                            )";
            var parameters = new DynamicParameters(new { name = chat.Name, message = chat.Message});
            var isSqlInvalid = !await _databaseRepo.ExecuteSQL(chatSql, parameters);
            if (isSqlInvalid)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            var lastChat = await GetLastChat();
             if (lastChat == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(lastChat);
        }

        private async Task<ChatDTO> GetLastChat()
        {
            var getLastRowSql = @$"
                    SELECT TOP(1) *   
                    FROM [dbo].[{table}]   
                    ORDER BY datetime DESC";
            var chats = await _databaseRepo.QuerySQL<ChatDTO>(getLastRowSql);
            return chats.FirstOrDefault();
        }

        private async void CreateChatTable()
        {
            var createChatTableSql = @$"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{table}' AND xtype='U')
                CREATE TABLE {table} (
					id int NOT NULL IDENTITY,
                    name TEXT NOT NULL,
                    message TEXT NOT NULL,
                    datetime DATETIME NOT NULL
                )";
            await _databaseRepo.ExecuteSQL(createChatTableSql);
        }
    }
}
