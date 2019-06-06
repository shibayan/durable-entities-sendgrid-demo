using System.Collections.Generic;

using Microsoft.Azure.WebJobs;

namespace MailSender.Entities
{
    public class MailStatusEntity
    {
        [FunctionName(nameof(MailStatusEntity))]
        public void EntryPoint([EntityTrigger] IDurableEntityContext context)
        {
            var state = context.GetState(() => new HashSet<string>());

            switch (context.OperationName)
            {
                case "Add":
                    {
                        var mailId = context.GetInput<string>();

                        state.Add(mailId);
                    }
                    break;
                case "Remove":
                    {
                        var mailId = context.GetInput<string>();

                        state.Remove(mailId);
                    }
                    break;
            }
        }
    }
}