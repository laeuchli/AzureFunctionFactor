using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Http;

using Newtonsoft.Json;


using System;

public class FactorFunction
{
    public class FactoringResult
    {

        public string id { get; set; }
        public string Factor1 { get; set; }
        public string Factor2 { get; set; }
        public DateTime Timestamp { get; set; }
    }

    private CosmosClient _client;
    Database _databaseFactors;
    Microsoft.Azure.Cosmos.Container _containerFactors;
    bool _dbInit;
 
    public FactorFunction(CosmosClient cosmosClient)
    {
        _client = cosmosClient;
    }
    public async Task<bool> CreateDataBase()
    {
        if (_dbInit == false)
        {
            _databaseFactors = await _client.CreateDatabaseIfNotExistsAsync(id: "Factors");
            _containerFactors = await _databaseFactors.CreateContainerIfNotExistsAsync(id: "Input", partitionKeyPath: "/id");


            _dbInit = true;
        }
        return true;
    }
    public async Task SaveFactoringResultAsync(FactoringResult result)
    {
        await _containerFactors.CreateItemAsync(result, new PartitionKey(result.id));
    }

    [Function("FindFactors")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        //string numberString = WebUtility.UrlDecode(query["number"]);

        string numberString = null;
        var query = req.Url.Query.TrimStart('?').Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in query)
        {
            // Then split on '=' (use char[], limit to 2 pieces)
            var kv = pair.Split(new[] { '=' }, 2);
            if (kv.Length == 2 && kv[0] == "number")
            {
                numberString = System.Net.WebUtility.UrlDecode(kv[1]);
                break;
            }
        }
        //  var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        // string numberString = query["number"];

        if (!BigInteger.TryParse(numberString, out BigInteger number) || number <= 1)
        {
            var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Invalid number");
            return badResponse;
        }

        var result = FindFactors(number);

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);

        bool database = await CreateDataBase();

        var result2 = new FactoringResult
        {
            id = number.ToString(),
            Factor1 = result.Value.Item1.ToString(),
            Factor2 = result.Value.Item2.ToString(),
            Timestamp = DateTime.UtcNow
        };
        try
        {
            await SaveFactoringResultAsync(result2);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {


        }

        if (result != null)
        {
            await response.WriteAsJsonAsync(new { factor1 = result.Value.Item1.ToString(), factor2 = result.Value.Item2.ToString() });
        }
        else
        {
            await response.WriteAsJsonAsync(new { message = "No non-trivial factors found." });
        }

        return response;
    }

    private static (BigInteger, BigInteger)? FindFactors(BigInteger number)
    {
        BigInteger limit = Sqrt(number) + 1;

        for (BigInteger i = 2; i <= limit; i++)
        {
            if (number % i == 0)
                return (i, number / i);
        }

        return null;
    }

    private static BigInteger Sqrt(BigInteger n)
    {
        if (n < 0) throw new ArgumentException("Negative input");
        if (n == 0) return 0;

        BigInteger root = n / 2;
        BigInteger last;

        do
        {
            last = root;
            root = (root + n / root) / 2;
        } while (root != last);

        return root;
    }
}