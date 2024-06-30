using System.IO;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;

public class AuthenticateUser
{
    private readonly IConfiguration _configuration;

    public AuthenticateUser(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [Function("AuthenticateUser")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "authenticate_user")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("AuthenticateUser");
        logger.LogInformation("Authenticating user.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);

        string email = data.email;
        string password = data.password;

        string connectionString = _configuration.GetValue<string>("MySqlConnectionString");

        bool isAuthenticated = false;

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var query = "SELECT COUNT(*) FROM usuarios WHERE correo = @Email AND contrasena = @Password";
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Password", password);

                isAuthenticated = (long)await cmd.ExecuteScalarAsync() > 0;
            }
        }

        var response = req.CreateResponse(isAuthenticated ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.Unauthorized);
        await response.WriteStringAsync(isAuthenticated ? "Authenticated successfully" : "Authentication failed");

        return response;
    }
}
