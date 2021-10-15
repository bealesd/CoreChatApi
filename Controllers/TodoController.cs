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
    public class TodoController : ControllerBase
    {
        private readonly string table = "todo";
        private readonly ILogger<TodoController> _logger;
        private readonly string _dbConnectionString;
        private DatabaseRepo _databaseRepo { get; set; }
        public SqlLogger _myLogger { get; set; }

        public TodoController(IConfiguration config, ILogger<TodoController> logger)
        {
            _dbConnectionString = config.GetConnectionString("db");

            _logger = logger;
            _logger.LogInformation($"Connection string:\n\t{_dbConnectionString}");

            _myLogger = new SqlLogger(_dbConnectionString);

            _databaseRepo = new DatabaseRepo(_dbConnectionString, _myLogger);

            Task.Run(() => this.CreateTodoTable()).Wait();
        }

        [HttpGet]
        [ActionName("GetTodos")]
        [Produces("application/json")]
        public async Task<IActionResult> GetTodos()
        {
            var getTodosSql = @$"SELECT * FROM [dbo].[{table}]";

            var todos = await _databaseRepo.QuerySQL<TodoDTO>(getTodosSql);
            if (todos == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(todos);
        }

        [HttpPost]
        [ActionName("AddTodo")]
        public async Task<IActionResult> AddTodo(TodoDTO todo)
        {
            var todoSql = @$"USE [CoreChat]
                INSERT INTO [dbo].[{table}](
                            [text],
                            [complete],
                            [datetime]
                            )
                        VALUES(
                            @text,
                            @complete,
                            GETDATE()
                            )";
            var parameters = new DynamicParameters(new
            {
                text = todo.Text,
                complete = todo.Complete,
                datetime = todo.DateTime
            });
            var isSqlInvalid = !await _databaseRepo.ExecuteSQL(todoSql, parameters);
            if (isSqlInvalid)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            var lastTodo = await GetLastTodo();
            if (lastTodo == null)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok(lastTodo);
        }

        [HttpPut]
        [ActionName("UpdateTodo")]
        public async Task<IActionResult> UpdateTodo(TodoDTO todo)
        {
            var todoSql = @$"USE [CoreChat]
                UPDATE [dbo].[{table}]
                SET complete = @complete
                WHERE id = @id";
            var parameters = new DynamicParameters(new
            {
                id = todo.Id,
                complete = todo.Complete,
            });
            var isSqlInvalid = !await _databaseRepo.ExecuteSQL(todoSql, parameters);
            if (isSqlInvalid)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok();
        }

        private async Task<TodoDTO> GetLastTodo()
        {
            var getLastRowSql = @$"
                    SELECT TOP(1) *   
                    FROM [dbo].[{table}]   
                    ORDER BY id DESC";
            var records = await _databaseRepo.QuerySQL<TodoDTO>(getLastRowSql);
            return records.FirstOrDefault();
        }

        [HttpDelete]
        [ActionName("DeleteTodo")]
        public async Task<IActionResult> DeleteTodo(int id)
        {
            var deleteTodoSql = @$"
                    DELETE
                    FROM [dbo].[{table}]   
                    WHERE id = @id";

            var parameters = new DynamicParameters(new { id = id });
            var result = await _databaseRepo.ExecuteSQL(deleteTodoSql, parameters);
            if (!result)
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, Globals.FAILED_TO_EXECUTE_SQL);

            return Ok();
        }

        private async void CreateTodoTable()
        {
            var createTodoTableSql = @$"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{table}' AND xtype='U')
                CREATE TABLE {table} (
					id INT NOT NULL IDENTITY,
                    text VARCHAR(MAX) NOT NULL,
                    complete INT NOT NULL,
                    datetime DATETIME NOT NULL
                )";
            await _databaseRepo.ExecuteSQL(createTodoTableSql);
        }
    }
}
