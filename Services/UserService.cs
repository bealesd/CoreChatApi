using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using CoreChatApi.Helpers;
using CoreChatApi.Dtos;
using System.Security.Cryptography;
using CoreChatApi.Logger;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Dapper;
using CoreChatApi.Repos;

namespace CoreChatApi.Services
{
    public interface IUserService
    {
        Task<string> Authenticate(UserLoginDTO model);
        bool ValidateUser(string hashAndSalt, string password);
        string CreateHashAndSalt(string password);
    }

    public class UserService : IUserService
    {
        public SqlLogger _myLogger { get; set; }
        private DatabaseRepo _databaseRepo { get; set; }
        private readonly ILogger<UserService> _logger;
        private readonly string _dbConnectionString;
        private readonly string table = "auth";

        private readonly AppSettings _appSettings;

        public UserService(
            IOptions<AppSettings> appSettings,
            IConfiguration config,
            ILogger<UserService> logger
        )
        {
            _appSettings = appSettings.Value;

            _dbConnectionString = config.GetConnectionString("db");

            _logger = logger;
            _logger.LogInformation($"Connection string:\n\t{_dbConnectionString}");

            _myLogger = new SqlLogger(_dbConnectionString);

            _databaseRepo = new DatabaseRepo(_dbConnectionString, _myLogger);

        }

        public async Task<string> Authenticate(UserLoginDTO model)
        {
            var sqlUser = await GetUser(model.Username);
            if (sqlUser == null)
            {
                await _myLogger.LogMessage(Globals.FAILED_TO_GET_USER, "error");
                return null;
            }

            if (!ValidateUser(sqlUser.Hash, model.Password))
            {
                await _myLogger.LogMessage(Globals.FAILED_TO_LOGIN, "error");
                return null;
            }

            var user = new UserLoginDTO() { Username = sqlUser.Username, Id = sqlUser.Id, Role = sqlUser.Role };
            var token = generateJwtToken(user);

            return token;
        }

        private string generateJwtToken(UserLoginDTO user)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[] {
                     new Claim("id", user.Id.ToString()),
                     new Claim("role", user.Role)
                     }),
                    Expires = DateTime.UtcNow.AddDays(7),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
            }
            catch (System.Exception ex)
            {
                return "";
            }

        }

        public string CreateHashAndSalt(string password)
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

        public bool ValidateUser(string hashAndSalt, string password)
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

        private async Task<UserSqlDTO> GetUser(string username)
        {
            var getUserSql = @$"
                    SELECT *   
                    FROM [dbo].[{table}]   
                    WHERE username LIKE @username";

            var parameters = new DynamicParameters(new { username = username });
            var result = await _databaseRepo.QuerySQL<UserSqlDTO>(getUserSql, parameters);
            if (result == null)
            {
                await _myLogger.LogMessage(Globals.FAILED_TO_EXECUTE_SQL, "error");
                return null;
            }

            var sqlUser = result.ToList().FirstOrDefault();
            if (sqlUser == null)
                await _myLogger.LogMessage(Globals.FAILED_TO_FIND_USER, "error");

            return sqlUser;
        }

    }
}