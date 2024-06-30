using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Data;

public class BeanFunctions
{
    private readonly IConfiguration _configuration;

    public BeanFunctions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [Function("BeanFunctions")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("BeanFunctions");
        logger.LogInformation("Processing a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);
        string operation = data.operation;

        string connectionString = _configuration.GetValue<string>("MySqlConnectionString");

        switch (operation)
        {
            case "create":
                return await CreateBean(data, req, connectionString);
            case "read":
                return await GetBeans(req, connectionString);
            case "update":
                return await UpdateBean(data, req, connectionString);
            case "delete":
                return await DeleteBean(data, req, connectionString);
            case "getByLocation":
                int locationId = data.locationId;
                return await GetBeansByLocation(req, connectionString, locationId);
            default:
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Invalid operation");
                return errorResponse;
        }
    }

    private async Task<HttpResponseData> CreateBean(dynamic data, HttpRequestData req, string connectionString)
    {
        int locationId = data.locationId;
        string description = data.description;

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var query = "INSERT INTO botes_tapas_plasticas (ubicacion_id, descripcion) VALUES (@locationId, @description)";
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@locationId", locationId);
                cmd.Parameters.AddWithValue("@description", description);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteStringAsync("Bean created successfully");

        return response;
    }

    private async Task<HttpResponseData> GetBeans(HttpRequestData req, string connectionString)
    {
        var beanList = new List<dynamic>();

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var query = "SELECT * FROM botes_tapas_plasticas";
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        beanList.Add(new
                        {
                            Id = reader.GetInt32("id"),
                            LocationId = reader.GetInt32("ubicacion_id"),
                            Description = reader.GetString("descripcion")
                        });
                    }
                }
            }
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(beanList);

        return response;
    }

    private async Task<HttpResponseData> UpdateBean(dynamic data, HttpRequestData req, string connectionString)
    {
        int id = data.id;
        int locationId = data.locationId;
        string description = data.description;

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var query = "UPDATE botes_tapas_plasticas SET ubicacion_id = @locationId, descripcion = @description WHERE id = @id";
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@locationId", locationId);
                cmd.Parameters.AddWithValue("@description", description);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteStringAsync("Bean updated successfully");

        return response;
    }

    private async Task<HttpResponseData> DeleteBean(dynamic data, HttpRequestData req, string connectionString)
    {
        int id = data.id;

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var query = "DELETE FROM botes_tapas_plasticas WHERE id = @id";
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteStringAsync("Bean deleted successfully");

        return response;
    }

    private async Task<HttpResponseData> GetBeansByLocation(HttpRequestData req, string connectionString, int locationId)
    {
        var beanList = new List<dynamic>();

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var query = "SELECT * FROM botes_tapas_plasticas WHERE ubicacion_id = @locationId";
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@locationId", locationId);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        beanList.Add(new
                        {
                            Id = reader.GetInt32("id"),
                            LocationId = reader.GetInt32("ubicacion_id"),
                            Description = reader.GetString("descripcion")
                        });
                    }
                }
            }
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(beanList);

        return response;
    }
}
