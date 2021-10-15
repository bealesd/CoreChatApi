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
        }

        [Authorize]
        [HttpGet]
        [ActionName("GetLogs")]
        [Produces("application/json")]
        public async Task<IActionResult> GetLogs()
        {
            var logs = (await _myLogger.GetLogs()).ToList();
            if (logs == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(logs);
        }

        [Authorize]
        [HttpPost]
        [ActionName("AddLog")]
        public async Task<IActionResult> AddLog(LogDTO log)
        {
            var isSqlInvalid = !await _myLogger.LogMessage(log.Message, log.Level);

            if (isSqlInvalid)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            var lastLog = await _myLogger.GetLastLog();
             if (lastLog == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(lastLog);
        }
    }
}
