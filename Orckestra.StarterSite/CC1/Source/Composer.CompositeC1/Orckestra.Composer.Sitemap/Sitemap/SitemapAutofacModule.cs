﻿using System;
using Autofac;
using Orckestra.Composer.CompositeC1.Providers;
using Orckestra.Composer.CompositeC1.Services;
using Orckestra.Composer.Providers;
using Orckestra.Composer.Services;
using Orckestra.Composer.Sitemap;
using Orckestra.Composer.Sitemap.Config;
using Orckestra.Composer.Sitemap.Product;
using Orckestra.Overture;
using System.Web.Hosting;
using Composite.Core;
using Orckestra.Composer.Sitemap.Factory;
using Orckestra.ExperienceManagement.Configuration;
using Orckestra.ExperienceManagement.Configuration.Settings;

namespace Orckestra.Composer.CompositeC1.Sitemap
{
    public class SitemapAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            if (C1SitemapConfiguration.IsScheduleDefined)
            {
                RegisterSitemapGenerationDependencies(builder);
            }
        }

        private static void RegisterSitemapGenerationDependencies(ContainerBuilder builder)
        {
            // Register overture client + product url provider + scope provider + culture service
            builder.Register(container => ComposerOvertureClient.CreateFromConfig()).As<IOvertureClient>().SingleInstance();

            builder.RegisterType<WebsiteProductUrlProvider>().As<IProductUrlProvider>();
            builder.RegisterType<ProductUrlParamFactory>().As<IProductUrlParamFactory>();
            builder.RegisterType<SiteConfiguration>().As<ISiteConfiguration>();
            builder.RegisterType<PageService>().As<IPageService>();

            builder.Register(container => ServiceLocator.GetService<ICookieAccesserSettings>()).SingleInstance();
            builder.Register(container => ServiceLocator.GetService<ICdnDamProviderSettings>()).SingleInstance();

            builder.RegisterType<ScopeProvider>().As<IScopeProvider>().SingleInstance();
            builder.RegisterType<CultureService>().As<ICultureService>().SingleInstance();

            var sitemapGeneratorConfig = new SitemapGeneratorConfig(
                HostingEnvironment.MapPath(C1SitemapConfiguration.SitemapDirectory),
                HostingEnvironment.MapPath(C1SitemapConfiguration.WorkingDirectory)
            );

            // C1 content sitemap provider
            builder.RegisterType<C1ContentSitemapEntryProvider>().AsSelf().SingleInstance();
            builder.RegisterType<C1ContentSitemapNamer>().AsSelf().SingleInstance();
            builder.RegisterType<C1ContentSitemapProviderConfig>().AsSelf().SingleInstance();
            builder.RegisterType<C1ContentSitemapProvider>().As<ISitemapProvider>().SingleInstance();
            builder.RegisterType<C1ContentSitemapPageExcludeProvider>().AsSelf().SingleInstance();

            // Product sitemap provider             
            builder.Register(container => new ProductSitemapEntryProvider(
                container.Resolve<IOvertureClient>(),
                container.Resolve<IProductUrlProvider>(),
                container.Resolve<IProductUrlParamFactory>()
            )).AsSelf().SingleInstance();
            builder.RegisterType<ProductSitemapNamer>().AsSelf().SingleInstance();
            builder.RegisterType<ProductSitemapProviderConfig>().AsSelf().SingleInstance();
            builder.RegisterType<ProductSitemapProvider>().As<ISitemapProvider>().SingleInstance();

            // Sitemap index generator
            builder.RegisterType<SitemapIndexGenerator>().As<ISitemapIndexGenerator>().SingleInstance();

            // Sitemap Generator   
            builder.Register(container => sitemapGeneratorConfig).As<ISitemapGeneratorConfig>().SingleInstance();
            builder.RegisterType<SitemapGenerator>().AsSelf().As<ISitemapGenerator>().SingleInstance();
        }
    }
}
