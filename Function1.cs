using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DurableDelete
{
    public static class Function1
    {
        [Function(nameof(Function1))]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(Function1));
            logger.LogInformation("Saying hello.");
            var outputs = new List<string>();

            // Replace name and input with values relevant for your Durable Functions Activity
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [Function(nameof(MattOrchestrator))]
        public static async Task<List<string>> MattOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(MattOrchestrator));

            logger.LogInformation("MattOrchestrator started.");

            var outputs = new List<string>();

            logger.LogInformation(context.GetInput<string>());

            var rawInputData = context.GetInput<string>();

            dynamic data = JsonConvert.DeserializeObject(rawInputData);

            var inputData = (((dynamic)data)["input"]).ToString();

            inputData = JsonConvert.DeserializeObject(inputData);

            var activityName = ((dynamic)inputData)["activity"].Value;

            var activityInput = ((dynamic)inputData)["data"].Value;

            outputs.Add(await context.CallActivityAsync<string>(activityName, activityInput));

            return outputs;
        }

        [Function(nameof(MattActivity))]
        public static string MattActivity([ActivityTrigger] string input, FunctionContext executionContext)
        {
            var activityInput = executionContext.BindingContext.BindingData["data"];

            ILogger logger = executionContext.GetLogger("MattActivity");
            logger.LogInformation("MattActivity with this input: {input}.", activityInput);
            return $"Input is {input}!";
        }

        [Function(nameof(SayHello))]
        public static string SayHello([ActivityTrigger] string name, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("SayHello");
            logger.LogInformation("Saying hello to {name}.", name);
            return $"Hello {name}!";
        }

        [Function("Function1_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("Function1_HttpStart");

            string name = String.Empty;
            string input = String.Empty;

            string requestBody = String.Empty;
            using (StreamReader streamReader = new StreamReader(req.Body))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = ((dynamic)data)["name"];
            input = (((dynamic)data)["input"]).ToString();

            // convert data to JObject
            //JObject jObject = JObject.Parse(data);

            // Function input comes from the request content.
            //string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(name);

            // New orchestration instance with input
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(name, requestBody);

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return client.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
