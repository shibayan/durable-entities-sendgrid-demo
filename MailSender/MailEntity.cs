using System;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;

using SendGrid;
using SendGrid.Helpers.Mail;

namespace FunctionApp63
{
    public class MailEntity
    {
        [FunctionName(nameof(MailEntity))]
        public async Task EntryPoint([EntityTrigger] IDurableEntityContext context)
        {
            switch (context.OperationName)
            {
                case "Send":
                    await Send(context);
                    break;
                case "Resend":
                    await Resend(context);
                    break;
                case "UpdateStatus":
                    await UpdateStatus(context);
                    break;
            }
        }

        public MailMessage Message { get; set; }
        public string Status { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }

        private async Task Send(IDurableEntityContext context)
        {
            var message = context.GetInput<MailMessage>();

            var sendGrid = new SendGridClient(Environment.GetEnvironmentVariable("SENDGRID_APIKEY"));

            var sendGridMessage = new SendGridMessage
            {
                From = new EmailAddress("me@shibayan.jp"),
                Subject = message.Subject,
                PlainTextContent = message.Body
            };

            sendGridMessage.AddTo(message.To);
            sendGridMessage.AddCustomArg("entitykey", context.Key);

            await sendGrid.SendEmailAsync(sendGridMessage);

            context.SetState(new MailEntity { Message = message, SentAt = DateTime.Now });
        }

        private async Task Resend(IDurableEntityContext context)
        {
            var state = context.GetState<MailEntity>();

            var sendGrid = new SendGridClient(Environment.GetEnvironmentVariable("SENDGRID_APIKEY"));

            var sendGridMessage = new SendGridMessage
            {
                From = new EmailAddress(Environment.GetEnvironmentVariable("FROM_ADDRESS")),
                Subject = state.Message.Subject,
                PlainTextContent = state.Message.Body
            };

            sendGridMessage.AddTo(state.Message.To);
            sendGridMessage.AddCustomArg("entitykey", context.Key);

            await sendGrid.SendEmailAsync(sendGridMessage);

            context.SignalEntity(new EntityId(nameof(MailStatusEntity), state.Status), "Remove", context.Key);

            state.Status = "resending";
            state.LastUpdatedAt = DateTime.Now;

            context.SignalEntity(new EntityId(nameof(MailStatusEntity), state.Status), "Add", context.Key);

            context.SetState(state);
        }

        private Task UpdateStatus(IDurableEntityContext context)
        {
            var state = context.GetState<MailEntity>();

            var status = context.GetInput<string>();

            if (state.Status != null)
            {
                context.SignalEntity(new EntityId(nameof(MailStatusEntity), state.Status), "Remove", context.Key);
            }

            if (state.Status != status)
            {
                context.SignalEntity(new EntityId(nameof(MailStatusEntity), status), "Add", context.Key);
            }

            state.Status = status;
            state.LastUpdatedAt = DateTime.Now;

            context.SetState(state);

            return Task.CompletedTask;
        }
    }
}