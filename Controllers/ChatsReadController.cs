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
    public class ChatsReadController : ControllerBase
    {
        private readonly string table = "chatsRead";
        private readonly ILogger<ChatController> _logger;
        private readonly string _dbConnectionString;
        private DatabaseRepo _databaseRepo { get; set; }
        public SqlLogger _myLogger { get; set; }

        public ChatsReadController(IConfiguration config, ILogger<ChatController> logger)
        {
            _dbConnectionString = config.GetConnectionString("db");

            _logger = logger;
            _logger.LogInformation($"Connection string:\n\t{_dbConnectionString}");

            _myLogger = new SqlLogger(_dbConnectionString);

            _databaseRepo = new DatabaseRepo(_dbConnectionString, _myLogger);

            CreateChatsReadTable();
        }

        // [Authorize]
        [HttpGet]
        [ActionName("GetChatsThatAreRead")]
        [Produces("application/json")]
        public async Task<IActionResult> GetChatsThatAreRead(int usernameId, string chatIds)
        {
            var getChatReadSql = @$"
                    SELECT *   
                    FROM [dbo].[{table}]   
                    WHERE usernameId = {usernameId}
                    AND chatId in ({chatIds})
                    ORDER BY datetime DESC";

            var getChatRead = (await _databaseRepo.QuerySQL<ChatReadDTO>(getChatReadSql)).ToList();
            if (getChatRead == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(getChatRead);
        }

        // [Authorize]
        [HttpPost]
        [ActionName("AddChatRead")]
        public async Task<IActionResult> AddChatRead(ChatReadDTO chatRead)
        {
            var insertChatReadSql = @$"USE [CoreChat]
                INSERT INTO [dbo].[{table}](
                            [usernameId],
                            [chatId],
                            [datetime]
                            )
                        VALUES(
                            @usernameId,
                            @chatId,
                            GETDATE()
                            )";
            var parameters = new DynamicParameters(new { usernameId = chatRead.UsernameId, chatId = chatRead.ChatId });
            var isSqlInvalid = !await _databaseRepo.ExecuteSQL(insertChatReadSql, parameters);
            if (isSqlInvalid)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok();
        }

        private async void CreateChatsReadTable()
        {
            var createChatsReadTableSql = @$"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{table}' AND xtype='U')
                CREATE TABLE {table} (
					id int NOT NULL IDENTITY,
                    usernameId int NOT NULL,
                    chatId int NOT NULL,
                    datetime DATETIME NOT NULL
                )";
            await _databaseRepo.ExecuteSQL(createChatsReadTableSql);
        }
    }
}
