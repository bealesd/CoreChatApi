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
    public class CalendarController : ControllerBase
    {
        private readonly string table = "calendar";
        private readonly ILogger<CalendarController> _logger;
        private readonly string _dbConnectionString;
        private DatabaseRepo _databaseRepo { get; set; }
        public SqlLogger _myLogger { get; set; }

        public CalendarController(IConfiguration config, ILogger<CalendarController> logger)
        {
            _dbConnectionString = config.GetConnectionString("db");

            _logger = logger;
            _logger.LogInformation($"Connection string:\n\t{_dbConnectionString}");

            _myLogger = new SqlLogger(_dbConnectionString);

            _databaseRepo = new DatabaseRepo(_dbConnectionString, _myLogger);

            CreateCalendarTable();
        }

        [HttpGet]
        [ActionName("GetRecords")]
        [Produces("application/json")]
        public async Task<IActionResult> GetRecords(int year, int month)
        {
            var getRecorsdsSql = @$"
                    SELECT *   
                    FROM [dbo].[{table}]   
                    WHERE year = @year
                    AND month = @month";

            var parameters = new DynamicParameters(new { year = year, month = month });
            var records = await _databaseRepo.QuerySQL<CalendarDTO>(getRecorsdsSql, parameters);
            if (records == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(records);
        }

        [HttpGet]
        [ActionName("GetAllRecords")]
        [Produces("application/json")]
        public async Task<IActionResult> GetAllRecords()
        {
            var getRecorsdsSql = @$"SELECT * FROM [dbo].[{table}]";

            var records = await _databaseRepo.QuerySQL<CalendarDTO>(getRecorsdsSql);
            if (records == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(records);
        }

        [HttpPost]
        [ActionName("AddRecord")]
        public async Task<IActionResult> AddRecord(CalendarDTO calendar)
        {
            var calendarSql = @$"USE [CoreChat]
                INSERT INTO [dbo].[{table}](
                            [what],
                            [year],
                            [month],
                            [day],
                            [hour],
                            [minute]
                            )
                        VALUES(
                            @what,
                            @year,
                            @month,
                            @day,
                            @hour,
                            @minute
                            )";
            var parameters = new DynamicParameters(new
            {
                what = calendar.What,
                year = calendar.Year,
                month = calendar.Month,
                day = calendar.Day,
                hour = calendar.Hour,
                minute = calendar.Minute
            });
            var isSqlInvalid = !await _databaseRepo.ExecuteSQL(calendarSql, parameters);
            if (isSqlInvalid)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            var lastRecord = await GetLastRecord();
            if (lastRecord == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(lastRecord);
        }

        [HttpPut]
        [ActionName("UpdateRecord")]
        public async Task<IActionResult> UpdateRecord(CalendarDTO calendar)
        {
            var calendarSql = @$"USE [CoreChat]
                UPDATE [dbo].[{table}]
                SET what = @what,
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

        private async Task<CalendarDTO> GetLastRecord()
        {
            var getLastRowSql = @$"
                    SELECT TOP(1) *   
                    FROM [dbo].[{table}]   
                    ORDER BY id DESC";
            var records = await _databaseRepo.QuerySQL<CalendarDTO>(getLastRowSql);
            return records.FirstOrDefault();
        }

        [HttpPost]
        [ActionName("DeleteRecord")]
        public async Task<IActionResult> DeleteRecord(int id)
        {
            var deleteRecordSql = @$"
                    DELETE *   
                    FROM [dbo].[{table}]   
                    WHERE id = @id";

            var parameters = new DynamicParameters(new { id = id });
            var result = await _databaseRepo.ExecuteSQL(deleteRecordSql, parameters);
            if (!result)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok();
        }

        private async void CreateCalendarTable()
        {
            var createCalendarTableSql = @$"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{table}' AND xtype='U')
                CREATE TABLE {table} (
					id INT NOT NULL IDENTITY,
                    what TEXT NOT NULL,
                    year INT NOT NULL,
                    month INT NOT NULL,
                    day INT NOT NULL,
                    hour INT NOT NULL,
                    minute INT NOT NULL,
                )";
            await _databaseRepo.ExecuteSQL(createCalendarTableSql);
        }
    }
}
