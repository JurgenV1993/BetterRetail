using System;
using System.Threading;
using System.Threading.Tasks;
using Composite.Core;
using Microsoft.ServiceBus.Messaging;
using Orckestra.Composer.CompositeC1.Services;
using Orckestra.Composer.CompositeC1.Sitemap;
using Orckestra.ExperienceManagement.Configuration.ServiceBus;

namespace Orckestra.Composer.Sitemap.ServiceBus
{
    public class SitemapGenerationEventMessageProcessor : IMessageProcessor
    {
        public string EventName { set; get; }

        public async Task ProcessMessageAsync(BrokeredMessage message, CancellationToken cancellationToken)
        {
            if (!message.ContentType.Contains(EventName)) return;

            var scheduler = ServiceLocator.GetService<IScheduler>();
            var multiSitemapGenerator = ServiceLocator.GetService<IMultiSitemapGenerator>();

            _ = Task.Run(() => scheduler.ScheduleTask(
                () => {
                    multiSitemapGenerator.GenerateSitemaps();
                },
                "SiteMap" + EventName,
                1
            ));
        }
    }
}
