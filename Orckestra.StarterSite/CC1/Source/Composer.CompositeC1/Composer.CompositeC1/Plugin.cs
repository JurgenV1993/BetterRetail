﻿using System;
using Orckestra.Composer.CompositeC1.Mappers;
using Orckestra.Composer.Cart.Providers.WishList;
using Orckestra.Composer.CompositeC1.Providers;
using Orckestra.Composer.CompositeC1.Services;
using Orckestra.Composer.HttpModules;
using Orckestra.Composer.Mvc.Sample.Providers.UrlProvider;
using Orckestra.Composer.Product.Providers;
using Orckestra.Composer.Providers;
using Orckestra.Composer.Search;
using Orckestra.Composer.Search.Providers;
using Orckestra.Composer.Store.Providers;
using Orckestra.Composer.Services;
using Orckestra.Composer.Services.Breadcrumb;
using Orckestra.Overture;

namespace Orckestra.Composer.CompositeC1
{
    public class Plugin : IComposerPlugin
    {
        public void Register(IComposerHost host)
        {
            RegisterDependencies(host);

            CategoriesConfiguration.CategoriesSyncConfiguration.Add("RootPageId", new Guid("f3dbd28d-365f-4d3e-91c3-7b730b39b294"));
        }

        private void RegisterDependencies(IComposerHost host)
        {
            host.Register<PageService, IPageService>();
            host.Register<CultureService, ICultureService>(ComponentLifestyle.Singleton);
            host.Register<HomeViewService, IHomeViewService>();
            host.Register<GoogleAnalyticsNavigationUrlProvider>();
            host.Register<NavigationMapper, INavigationMapper>();

            host.Register<ProductUrlProvider, IProductUrlProvider>();
            host.Register<SearchUrlProvider, ISearchUrlProvider>();
            host.Register<CategoryBrowsingUrlProvider, ICategoryBrowsingUrlProvider>();
            host.Register<CartUrlProvider, ICartUrlProvider>();
            host.Register<MyAccountUrlProvider, IMyAccountUrlProvider>();
            host.Register<WishListUrlProvider, IWishListUrlProvider>();            
            host.Register<RecurringScheduleUrlProvider, IRecurringScheduleUrlProvider>();
            host.Register<RecurringCartUrlProvider, IRecurringCartUrlProvider>();
            host.Register<CategoryPageService, ICategoryBrowsingService>();
            host.Register<ImageViewService, IImageViewService>();
            host.Register<MediaService, IMediaService>();
            host.Register<OrderUrlProvider, IOrderUrlProvider>();
            host.Register<Providers.StoreUrlProvider, IStoreUrlProvider>();
            host.Register<LanguageSwitchViewService, ILanguageSwitchService>();
            host.Register<BreadcrumbViewService, IBreadcrumbViewService>();
            // TODO: Why not done in Composer directly ??
            host.Register<SettingsFromConfigFileService, ISettingsService>();
            host.Register<MyAccountViewService, IMyAccountViewService>();
            host.Register<PageNotFoundUrlProvider, IPageNotFoundUrlProvider>();
            host.Register<AntiCookieTamperingExcluder, IAntiCookieTamperingExcluder>();
            host.Register<C1PerformanceDataCollector, IPerformanceDataCollector>();
        }
    }
}
