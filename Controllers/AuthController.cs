using System;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography;
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
    public class AuthController : ControllerBase
    {
        private readonly string table = "auth";
        private readonly ILogger<CalendarController> _logger;
        private readonly string _dbConnectionString;
        private DatabaseRepo _databaseRepo { get; set; }
        public SqlLogger _myLogger { get; set; }

        public AuthController(IConfiguration config, ILogger<CalendarController> logger)
        {
            _dbConnectionString = config.GetConnectionString("db");

            _logger = logger;
            _logger.LogInformation($"Connection string:\n\t{_dbConnectionString}");

            _myLogger = new SqlLogger(_dbConnectionString);

            _databaseRepo = new DatabaseRepo(_dbConnectionString, _myLogger);

            CreateUserTable();
        }

        [HttpPost]
        [ActionName("Login")]
        [Produces("application/json")]
        public async Task<IActionResult> Login(UserDTO user)
        {
            var sqlUser = await GetUser(user.Username);
            if(sqlUser == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_GET_USER);

            if (!ValidateUser(sqlUser.Hash, user.Password))
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_LOGIN);

            return Ok();
        }

        private string CreateHashAndSalt(string password)
        {
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            var hash = pbkdf2.GetBytes(20);

            byte[] hashBytes = new byte[36];

            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            return Convert.ToBase64String(hashBytes);
        }

        private bool ValidateUser(string hashAndSalt, string password)
        {
            var hashAndSaltBytes = Convert.FromBase64String(hashAndSalt);

            byte[] hashBytes = new byte[20];
            byte[] salt = new byte[16];

            Array.Copy(hashAndSaltBytes, 0, salt, 0, 16);
            Array.Copy(hashAndSaltBytes, 16, hashBytes, 0, 20);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            var hash = pbkdf2.GetBytes(20);

            return hashBytes.SequenceEqual(hash);
        }

        [HttpPost]
        [ActionName("AddUser")]
        public async Task<IActionResult> AddUser(UserDTO user)
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
                hashAndSalt = CreateHashAndSalt(user.Password)
            });

            var isSqlInvalid = !await _databaseRepo.ExecuteSQL(userSql, parameters);
            if (isSqlInvalid)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok();
        }

        private async Task<UserSqlDTO> GetUser(string username)
        {
            var getUserSql = @$"
                    SELECT *   
                    FROM [dbo].[{table}]   
                    WHERE username LIKE @username";
// WHERE CONVERT(VARCHAR, username) = @username";
            var parameters = new DynamicParameters(new { username = username });
            var result = await _databaseRepo.QuerySQL<UserSqlDTO>(getUserSql, parameters);
            if (result == null){
                await _myLogger.LogMessage(Globals.FAILED_TO_EXECUTE_SQL,"error");
                return null;
            }
                
            var sqlUser = result.ToList().FirstOrDefault();
            if (sqlUser == null)
                await _myLogger.LogMessage(Globals.FAILED_TO_FIND_USER,"error");

            return sqlUser;
        }

        [HttpPut]
        [ActionName("UpdateRecord")]
        public async Task<IActionResult> UpdateUser(CalendarDTO calendar)
        {
            var calendarSql = @$"USE [CoreChat]
                UPDATE [dbo].[{table}]
                SET what = @what,
                    description = @description, 
                    year = @year,
                    month = @month,
                    day = @day,
                    hour = @hour,
                    minute = @minute
                WHERE id = @id";
            var parameters = new DynamicParameters(new
            {
                id = calendar.Id,
                what = calendar.What,
                description = calendar.Description,
                year = calendar.Year,
                month = calendar.Month,
                day = calendar.Day,
                hour = calendar.Hour,
                minute = calendar.Minute
            });
            var isSqlInvalid = !await _databaseRepo.ExecuteSQL(calendarSql, parameters);
            if (isSqlInvalid)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok();
        }

        [HttpDelete]
        [ActionName("DeleteRecord")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var deleteRecordSql = @$"
                    DELETE
                    FROM [dbo].[{table}]   
                    WHERE id = @id";

            var parameters = new DynamicParameters(new { id = id });
            var result = await _databaseRepo.ExecuteSQL(deleteRecordSql, parameters);
            if (!result)
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
