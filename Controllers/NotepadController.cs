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
    public class NotepadController : ControllerBase
    {
        private readonly string table = "notepad";
        private readonly ILogger<TodoController> _logger;
        private readonly string _dbConnectionString;
        private DatabaseRepo _databaseRepo { get; set; }
        public SqlLogger _myLogger { get; set; }

        public NotepadController(IConfiguration config, ILogger<TodoController> logger)
        {
            _dbConnectionString = config.GetConnectionString("db");

            _logger = logger;
            _logger.LogInformation($"Connection string:\n\t{_dbConnectionString}");

            _myLogger = new SqlLogger(_dbConnectionString);

            _databaseRepo = new DatabaseRepo(_dbConnectionString, _myLogger);

            Task.Run(() => this.CreateNotepadTable()).Wait();
        }

        [HttpGet]
        [ActionName("GetNotepad")]
        [Produces("application/json")]
        public async Task<IActionResult> GetNotepad(int id)
        {
            var getNotepadsSql = @$"
                SELECT * FROM [dbo].[{table}]
                WHERE Id = @Id";

            var parameters = new DynamicParameters(new
            {
                Id = id
            });

            var notepad = (await _databaseRepo.QuerySQL<NotepadDTO>(getNotepadsSql, parameters)).FirstOrDefault();
            if (notepad == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(notepad);
        }


        [HttpGet]
        [ActionName("GetAllNotepads")]
        [Produces("application/json")]
        public async Task<IActionResult> GetAllNotepads()
        {
            var getTodosSql = @$"SELECT [Name], [Type], [Path], [Created], [Id] FROM [dbo].[{table}]";

            var notepads = await _databaseRepo.QuerySQL<NotepadDTO>(getTodosSql);
            if (notepads == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(notepads);
        }

        [HttpGet]
        [ActionName("GetNotepadDirectChildren")]
        [Produces("application/json")]
        public async Task<IActionResult> GetNotepadDirectChildren(string path)
        {
            // materialized path solution - http://www.dbazine.com/oracle/or-articles/tropashko4/

            var getNotepadsSql = @$"
                SELECT DISTINCT notepad1.Path, notepad1.Name, notepad1.Type, notepad1.Id, notepad1.Created
                FROM [dbo].[{table}] notepad1, [dbo].[{table}] notepad2
                WHERE notepad1.Path LIKE notepad2.Path 
                AND notepad2.Path LIKE @Path";

            var parameters = new DynamicParameters(new
            {
                Path = path
            });

            var notepads = await _databaseRepo.QuerySQL<NotepadDTO>(getNotepadsSql, parameters);
            if (notepads == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(notepads);
        }

        [HttpGet]
        [ActionName("GetNotepadChildren")]
        [Produces("application/json")]
        public async Task<IActionResult> GetNotepadChildren(string path)
        {
            var getNotepadsSql = @$"
                SELECT notepad1.Path, notepad1.Name, notepad1.Type, notepad1.Id, notepad1.Created
                FROM [dbo].[{table}] notepad1, [dbo].[{table}] notepad2
                WHERE notepad1.Path LIKE notepad2.Path + '%' 
                AND notepad2.Path = @Path";

            var parameters = new DynamicParameters(new
            {
                Path = path
            });

            var notepads = await _databaseRepo.QuerySQL<NotepadDTO>(getNotepadsSql, parameters);
            if (notepads == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(notepads);
        }

        [HttpGet]
        [ActionName("GetNotepadParents")]
        [Produces("application/json")]
        public async Task<IActionResult> GetNotepadParents(string path)
        {
            var getNotepadsSql = @$"
                SELECT notepad1.Path, notepad1.Name, notepad1.Type, notepad1.Id, notepad1.Created
                FROM [dbo].[{table}] notepad1, [dbo].[{table}] notepad2
                WHERE notepad2.Path LIKE notepad1.Path + '%' 
                AND notepad2.Path = @Path";

            var parameters = new DynamicParameters(new
            {
                Path = path
            });

            var notepads = await _databaseRepo.QuerySQL<NotepadDTO>(getNotepadsSql, parameters);
            if (notepads == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(notepads);
        }
        [HttpPost]
        [ActionName("AddNotepad")]
        public async Task<IActionResult> AddNotepad(NotepadDTO notepad)
        {
            var notepadSql = @$"USE [CoreChat]
                INSERT INTO [dbo].[{table}](
                            [Text],
                            [Name],
                            [Type],
                            [Path],
                            [Created]
                            )
                        VALUES(
                            @Text,
                            @Name,
                            @Type,
                            @Path,
                            GETDATE()
                            )";
            var parameters = new DynamicParameters(new
            {
                Text = notepad.Text,
                Name = notepad.Name,
                Type = notepad.Type,
                Path = notepad.Path
            });
            var isSqlInvalid = !await _databaseRepo.ExecuteSQL(notepadSql, parameters);
            if (isSqlInvalid)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            var lastNotepad = await GetLastNotepad();
            if (lastNotepad == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(lastNotepad);
        }

        [HttpPut]
        [ActionName("UpdateNotepad")]
        public async Task<IActionResult> UpdateNotepad(NotepadDTO notepad)
        {
            var notepadSql = "";
            if (notepad.Text == null)
            {
                notepadSql = @$"USE [CoreChat]
                UPDATE [dbo].[{table}]
                SET Path = @Path,
                Name = @Name
                WHERE id = @id";
            }
            else
            {
                notepadSql = @$"USE [CoreChat]
                UPDATE [dbo].[{table}]
                SET Text = @Text,
                Path = @Path,
                Name = @Name
                WHERE id = @id";
            }

            var parameters = new DynamicParameters(new
            {
                Id = notepad.Id,
                Text = notepad.Text,
                Path = notepad.Path,
                Type = notepad.Type,
                Name = notepad.Name
            });
            var isSqlInvalid = !await _databaseRepo.ExecuteSQL(notepadSql, parameters);
            if (isSqlInvalid)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok();
        }

        private async Task<NotepadDTO> GetLastNotepad()
        {
            var getLastRowSql = @$"
                    SELECT TOP(1) *   
                    FROM [dbo].[{table}]   
                    ORDER BY id DESC";
            var records = await _databaseRepo.QuerySQL<NotepadDTO>(getLastRowSql);
            return records.FirstOrDefault();
        }

        [HttpDelete]
        [ActionName("DeleteNotepad")]
        public async Task<IActionResult> DeleteNotepad(int id)
        {
            var deleteNotepadSql = @$"
                    DELETE
                    FROM [dbo].[{table}]   
                    WHERE id = @id";

            var parameters = new DynamicParameters(new { id = id });
            var result = await _databaseRepo.ExecuteSQL(deleteNotepadSql, parameters);
            if (!result)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok();
        }

        private async void CreateNotepadTable()
        {
            var creatNotepadTableSql = @$"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{table}' AND xtype='U')
                CREATE TABLE {table} (
					Id INT NOT NULL IDENTITY,
                    Text VARCHAR(MAX) NOT NULL,
                    Name VARCHAR(MAX) NOT NULL,
                    Type VARCHAR(MAX) NOT NULL,
                    Path VARCHAR(MAX) NOT NULL,
                    Created DATETIME NOT NULL
                )";
            await _databaseRepo.ExecuteSQL(creatNotepadTableSql);
        }
    }
}
