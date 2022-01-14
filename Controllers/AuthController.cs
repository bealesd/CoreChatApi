using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Dapper;
using CoreChatApi.Dtos;
using CoreChatApi.Repos;
using CoreChatApi.Logger;
using CoreChatApi.Services;

namespace CoreChatApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class AuthController : ControllerBase
    {
        private readonly string table = "auth";
        private IUserService _userService;
        private readonly ILogger<CalendarController> _logger;
        private readonly string _dbConnectionString;
        private DatabaseRepo _databaseRepo { get; set; }
        public SqlLogger _myLogger { get; set; }

        public AuthController(
            IConfiguration config,
             ILogger<CalendarController> logger,
             IUserService userService
             )
        {
            _dbConnectionString = config.GetConnectionString("db");

            _logger = logger;
            _logger.LogInformation($"Connection string:\n\t{_dbConnectionString}");

            _myLogger = new SqlLogger(_dbConnectionString);

            _databaseRepo = new DatabaseRepo(_dbConnectionString, _myLogger);

            CreateUserTable();

            _userService = userService;
        }

        [HttpPost]
        [ActionName("Login")]
        [Produces("application/json")]
        public async Task<IActionResult> Login(UserLoginDTO user)
        {
            var token = await _userService.Authenticate(user);

            if (token == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_GET_USER);

            return Ok(token);
        }

        [HttpGet]
        [ActionName("GetUsernameId")]
        public async Task<IActionResult> GetUsernameId(string username)
        {
            var getRecorsdSql = @$"
                SELECT *   
                FROM [dbo].[{table}]
                WHERE username LIKE @username";

            var parameters = new DynamicParameters(new { username = username });
            var records = await _databaseRepo.QuerySQL<UserLoginDTO>(getRecorsdSql, parameters);

            var usernameId = records.Select(profile => profile.Id).First();

            return Ok(usernameId);
        }

        // [Authorize]
        [HttpPost]
        [ActionName("AddUser")]
        public async Task<IActionResult> AddUser(UserLoginDTO user)
        {
            var userSql = @$"USE [CoreChat]
                INSERT INTO [dbo].[{table}](
                            [username],
                            [hash]
                            )
                        VALUES(
                            @username,
                            @hashAndSalt
                            )";


            var parameters = new DynamicParameters(new
            {
                username = user.Username,
                hashAndSalt = _userService.CreateHashAndSalt(user.Password)
            });

            var isSqlInvalid = !await _databaseRepo.ExecuteSQL(userSql, parameters);
            if (isSqlInvalid)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok();
        }

        private async void CreateUserTable()
        {
            var createCalendarTableSql = @$"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{table}' AND xtype='U')
                CREATE TABLE {table} (
                    id INT NOT NULL IDENTITY,
                    username VARCHAR(MAX) NOT NULL,
                    hash VARCHAR(MAX) NOT NULL,
                )";
            await _databaseRepo.ExecuteSQL(createCalendarTableSql);
        }
    }
}
