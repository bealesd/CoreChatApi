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

namespace CoreChatApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ChatGroupsController : ControllerBase
    {
        private string _chatGroupsTable { get; set; }
        private string _authTable { get; set; }
        private readonly ILogger<ChatGroupsController> _logger;
        private readonly string _dbConnectionString;
        private DatabaseRepo _databaseRepo { get; set; }
        public SqlLogger _myLogger { get; set; }

        public ChatGroupsController(IConfiguration config, ILogger<ChatGroupsController> logger)
        {
            _dbConnectionString = config.GetConnectionString("db");

            _chatGroupsTable = config.GetValue<string>("Tables:chatGroups");
            _authTable= config.GetValue<string>("Tables:auth");

            _logger = logger;
            _logger.LogInformation($"Connection string:\n\t{_dbConnectionString}");

            _myLogger = new SqlLogger(_dbConnectionString);

            _databaseRepo = new DatabaseRepo(_dbConnectionString, _myLogger);

            CreateChatGroupsTable();
        }

        [Authorize]
        [HttpGet("{usernameId}")]
        [ActionName("GetChatGroupsById")]
        [Produces("application/json")]
        public async Task<IActionResult> GetChatGroupsById(int usernameId)
        {
            var chatGuidsByUsernamesRelatedToUserSql = @$"
                    SELECT *FROM [dbo].[{_chatGroupsTable}] 
                    WHERE guid IN 
                        (SELECT guid FROM [dbo].[{_chatGroupsTable}]
                        WHERE usernameId=@usernameId);";

            var usernameParameter = new DynamicParameters(new { usernameId = usernameId });
            var chatGroupsRelatedToUser = await _databaseRepo.QuerySQL<ChatGroupDTO>(chatGuidsByUsernamesRelatedToUserSql, usernameParameter);

            if (chatGroupsRelatedToUser == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            var usernameIdsRelatedToUser = chatGroupsRelatedToUser.Select(a => a.UsernameId).Distinct();

            var sql = @$"SELECT * FROM [dbo].[{_authTable}] WHERE id IN @usernameIds";
            var usernameParameters = new DynamicParameters(new { usernameIds = usernameIdsRelatedToUser });
            var usernamesRelatedToUser = await _databaseRepo.QuerySQL<UserLoginDTO>(sql, usernameParameters);

            var chatGroupUsernameDTOs = new List<ChatGroupUsernameDTO>();
            foreach (var chatGroupRelatedToUser in chatGroupsRelatedToUser)
            {
                var username = usernamesRelatedToUser.Single(a => a.Id == chatGroupRelatedToUser.UsernameId).Username;

                if (chatGroupUsernameDTOs.Any(x => x.Guid == chatGroupRelatedToUser.Guid))
                {
                    chatGroupUsernameDTOs.Single(x => x.Guid == chatGroupRelatedToUser.Guid).Usernames.Add(username);
                }
                else
                {
                    chatGroupUsernameDTOs.Add(new ChatGroupUsernameDTO
                    {
                        Usernames = new List<string>() { username },
                        Guid = chatGroupRelatedToUser.Guid
                    });
                }
            }
            return Ok(chatGroupUsernameDTOs);
        }

        // [Authorize]
        [HttpPost]
        [ActionName("AddChatGroup")]
        public async Task<IActionResult> AddChatGroup(List<int> usernameIds)
        {
            var guid = System.Guid.NewGuid();
            List<string> sqlUsernameIds = new List<string>();
            foreach (var usernameId in usernameIds)
            {
                sqlUsernameIds.Add($"({usernameId},'{guid}')");
            }

            var sqlUsernameValues = System.String.Join(",", sqlUsernameIds);
            var chatGroupsSql = @$"
                USE [CoreChat]
                INSERT INTO [dbo].[{_chatGroupsTable}](
                            [usernameID], [guid]
                            )
                            VALUES {sqlUsernameValues}";

            var isSqlInvalid = !await _databaseRepo.ExecuteSQL(chatGroupsSql.ToString(), null);
            if (isSqlInvalid)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(guid);
        }

        private async Task<ChatGroupDTO> GetLastChatGroup()
        {
            var getLastRowSql = @$"
                    SELECT *   
                    FROM [dbo].[{_chatGroupsTable}]
                    WHERE id = ( SELECT MAX(id) FROM [dbo].[{_chatGroupsTable}])";
            var chats = await _databaseRepo.QuerySQL<ChatGroupDTO>(getLastRowSql);
            return chats.FirstOrDefault();
        }

        private async void CreateChatGroupsTable()
        {
            var createChatGroupTableSql = @$"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{_chatGroupsTable}' AND xtype='U')
                CREATE TABLE {_chatGroupsTable} (
					id INT NOT NULL IDENTITY,
                    usernameId INT NOT NULL,
                    guid UNIQUEIDENTIFIER NOT NULL
                )";
            await _databaseRepo.ExecuteSQL(createChatGroupTableSql);
        }

        //what chats should i see
        /*
        how do you find someone to chat to?
        1. click start new group
        2. get list of all people from profiles table, GetUsernameIds, [2,3]
        3. submit those people to AddChatGroup

        how do I check what groups david is in?
        1. i am user 'david'
        2. i look up david in chat groups lookup table
        3. i choose a chat group that has an id
        4. i get all chats of this group by using id on chats table

        */

    }
}
