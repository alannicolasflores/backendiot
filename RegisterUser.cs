using System.IO;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;

public class RegisterUser
{
    private readonly IConfiguration _configuration;

    public RegisterUser(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [Function("RegisterUser")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "register_user")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("RegisterUser");
        logger.LogInformation("Registering new user.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);

        string name = data.name;
        string email = data.email;
        string password = data.password;

        string connectionString = _configuration.GetValue<string>("MySqlConnectionString");

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var query = "INSERT INTO usuarios (nombre, correo, contrasena) VALUES (@name, @Email, @Password)";
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Password", password);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteStringAsync("User registered successfully");

        return response;
    }
}
