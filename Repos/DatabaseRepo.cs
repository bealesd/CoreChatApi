using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Dapper;

using CoreChatApi.Logger;

namespace CoreChatApi.Repos
{

    public class DatabaseRepo
    {
        private readonly SqlLogger _logger;
        private string _dbConnectionString { get; set; }

        public DatabaseRepo(string dbConnectionString)
        {
            _dbConnectionString = dbConnectionString;
            _logger = null;
        }

        public DatabaseRepo(string dbConnectionString, SqlLogger logger)
        {
            _dbConnectionString = dbConnectionString;
            _logger = logger;
        }

        // public void CreateTable<T>(T model){
        //     System.Reflection.FieldAttributes
        // }

        public async Task<IEnumerable<T>> QuerySQL<T>(string sql)
        {
            var isConnectionInvalid = !await TestConnection(_dbConnectionString);
            if (isConnectionInvalid) return new List<T>();

            try
            {
                using (var con = new SqlConnection(_dbConnectionString))
                {
                    return await con.QueryAsync<T>(sql);
                }
            }
            catch (Exception exception)
            {
                if (_logger != null)
                    await _logger.LogMessage($"{exception.Message}.\n{Globals.FAILED_TO_RUN_SQL_QUERY}. Sql query: {sql}", "error");

                return null;
            }
        }
        public async Task<bool> ExecuteSQL(string sql)
        {
            var isConnectionInvalid = !await TestConnection(_dbConnectionString);
            if (isConnectionInvalid) return false;

            try
            {
                using (var con = new SqlConnection(_dbConnectionString))
                {
                    await con.ExecuteAsync(sql);
                }
                return true;
            }
            catch (Exception exception)
            {
                if (_logger != null)
                    await _logger.LogMessage($"{exception.Message}.\n{Globals.FAILED_TO_EXECUTE_SQL}. Sql: {sql}", "error");

                return false;
            }
        }

        public async Task<bool> TestConnection(string connectionString)
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();
                    return true;
                }
            }
            catch (Exception exception)
            {
                if (_logger != null)
                    await _logger.LogMessage($"{exception.Message}.\n{Globals.FAILED_TO_CONNECT_TO_DATABASE}. Connection string: {connectionString}", "error");
                return false;
            }
        }
    }

}