using System;
using System.IO;
using System.Threading.Tasks;

using MailSender.Entities;
using MailSender.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

using Newtonsoft.Json;

namespace MailSender
{
    public static class Function1
    {
        [FunctionName(nameof(SendMail))]
        public static async Task<IActionResult> SendMail(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "mail/send")] MailMessage message,
            [OrchestrationClient] IDurableOrchestrationClient client)
        {
            await client.SignalEntityAsync(new EntityId(nameof(MailEntity), Guid.NewGuid().ToString()), "Send", message);

            return new OkResult();
        }

        [FunctionName(nameof(ResendMail))]
        public static async Task<IActionResult> ResendMail(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "mail/resend/{id}")] HttpRequest req,
            string id,
            [OrchestrationClient] IDurableOrchestrationClient client)
        {
            await client.SignalEntityAsync(new EntityId(nameof(MailEntity), id), "Resend");

            return new OkResult();
        }

        [FunctionName(nameof(GetMail))]
        public static async Task<IActionResult> GetMail(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "mail/get/{id}")] HttpRequest req,
            string id,
            [OrchestrationClient] IDurableOrchestrationClient client)
        {
            var response = await client.ReadEntityStateAsync<MailEntity>(new EntityId(nameof(MailEntity), id));

            return new OkObjectResult(response.EntityState);
        }

        [FunctionName(nameof(WebhookReceiver))]
        public static async Task<IActionResult> WebhookReceiver(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "webhook/receiver")] HttpRequest req,
            [OrchestrationClient] IDurableOrchestrationClient client)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var eventPayloads = JsonConvert.DeserializeObject<EventPayload[]>(requestBody);

            foreach (var payload in eventPayloads)
            {
                await client.SignalEntityAsync(new EntityId(nameof(MailEntity), payload.EntityKey), "UpdateStatus", payload.Event);
            }

            return new OkResult();
        }

        [FunctionName(nameof(GetStatus))]
        public static async Task<IActionResult> GetStatus(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "status/get/{status}")] HttpRequest req,
            string status,
            [OrchestrationClient] IDurableOrchestrationClient client)
        {
            var response = await client.ReadEntityStateAsync<string[]>(new EntityId(nameof(MailStatusEntity), status));

            return new OkObjectResult(response.EntityState);
        }
    }
}
