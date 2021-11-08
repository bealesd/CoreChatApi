using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Dapper;
using CoreChatApi.Dtos;
using CoreChatApi.Repos;
using CoreChatApi.Logger;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace CoreChatApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ProfileController : ControllerBase
    {
        private readonly string table = "profile";
        private readonly ILogger<CalendarController> _logger;
        private readonly string _dbConnectionString;
        private DatabaseRepo _databaseRepo { get; set; }
        public SqlLogger _myLogger { get; set; }

        public ProfileController(
            IConfiguration config,
            ILogger<CalendarController> logger
             )
        {
            _dbConnectionString = config.GetConnectionString("db");

            _logger = logger;
            _logger.LogInformation($"Connection string:\n\t{_dbConnectionString}");

            _myLogger = new SqlLogger(_dbConnectionString);

            _databaseRepo = new DatabaseRepo(_dbConnectionString, _myLogger);

            CreateProfileTable();
        }

        // [Authorize]
        [HttpGet]
        [ActionName("GetProfile")]
        public async Task<IActionResult> GetProfile(string username)
        {
            var getRecorsdsSql = @$"
                    SELECT *   
                    FROM [dbo].[{table}]   
                    WHERE username = @username";

            var parameters = new DynamicParameters(new { username = username });
            var record = (await _databaseRepo.QuerySQL<ProfileDTO>(getRecorsdsSql, parameters)).FirstOrDefault();
            if (record == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return File(record.Picture, record.PictureType, record.PictureName);
        }

        [Authorize]
        [HttpPost]
        [ActionName("AddProfile")]
        public async Task<IActionResult> AddProfile([FromForm]Profile profile)
        {
            var userSql = @$"USE [CoreChat]
                INSERT INTO [dbo].[{table}](
                            [username],
                            [picture],
                            [pictureType],
                            [pictureName]
                            )
                        VALUES(
                            @username,
                            @picture,
                            @pictureType,
                            @pictureName
                            )";

            byte[] bytes;

            using (var memoryStream = new MemoryStream())
            {
                await profile.Picture.CopyToAsync(memoryStream);
                bytes = memoryStream.ToArray();
            }

            var parameters = new DynamicParameters(new
            {
                username = profile.Username,
                picture = bytes,
                pictureType = profile.Picture.ContentType,
                pictureName = profile.Picture.FileName
            });

            var isSqlInvalid = !await _databaseRepo.ExecuteSQL(userSql, parameters);
            if (isSqlInvalid)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok();
        }

        private async void CreateProfileTable()
        {
            var createCalendarTableSql = @$"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{table}' AND xtype='U')
                CREATE TABLE {table} (
                    id INT NOT NULL IDENTITY,
                    username VARCHAR(20) NOT NULL,
                    picture VARBINARY(MAX) NOT NULL,
                    pictureType VARCHAR(20) NOT NULL,
                    pictureName VARCHAR(20) NOT NULL
                )";
            await _databaseRepo.ExecuteSQL(createCalendarTableSql);
        }
    }
}
