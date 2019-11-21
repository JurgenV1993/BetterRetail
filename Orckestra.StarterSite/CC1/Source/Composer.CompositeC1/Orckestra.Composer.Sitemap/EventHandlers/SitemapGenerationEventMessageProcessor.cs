using System.Threading;
using System.Threading.Tasks;
using System.Web.Helpers;
using Composite.Core;
using Microsoft.ServiceBus.Messaging;
using Orckestra.ExperienceManagement.Configuration.ServiceBus;

namespace Orckestra.Composer.Sitemap.ServiceBus
{
    public class SitemapGenerationEventMessageProcessor : IMessageProcessor
    {
        public string EventName { set; get; }

        public async Task ProcessMessageAsync(BrokeredMessage message, CancellationToken cancellationToken)
        {
            if (!message.ContentType.Contains(EventName)) return;

            
        }
    }
}
