﻿using System;
using System.Collections.Generic;
using System.Linq;
using Composite.Core;
using Orckestra.Composer.CompositeC1.Services;
using Orckestra.Composer.Configuration;
using Orckestra.Composer.Parameters;
using Orckestra.Composer.Providers;
using Orckestra.Composer.Providers.Checkout;
using Orckestra.Composer.Services;
using Orckestra.ExperienceManagement.Configuration;
using Orckestra.Overture.Caching;

namespace Orckestra.Composer.CompositeC1.Providers
{
    class CartUrlProvider : ICartUrlProvider
    {
        protected IPageService PageService { get; private set; }
        protected ICacheProvider CacheProvider { get; private set; }
        protected IComposerContext ComposerContext { get; private set; }

        public CartUrlProvider(IPageService pageService, ICacheProvider cacheProvider, IComposerContext composerContext)
        {
            if (pageService == null) { throw new ArgumentNullException("pageService"); }
            if (cacheProvider == null) { throw new ArgumentNullException("cacheProvider"); }

            PageService = pageService;
            CacheProvider = cacheProvider;
            ComposerContext = composerContext;
          
        }

        public string GetCartUrl(GetCartUrlParam parameters)
        {
            if (parameters == null) { throw new ArgumentNullException("parameters"); }
            if (parameters.CultureInfo == null) { throw new ArgumentException("parameters.CultureInfo is required", "parameters"); }

            var pagesConfiguration = SiteConfiguration.GetPagesConfiguration(parameters.CultureInfo, ComposerContext.WebsiteId);
            return PageService.GetPageUrl(pagesConfiguration.CartPageId, parameters.CultureInfo);
        }

        public string GetCheckoutSignInUrl(GetCartUrlParam parameters)
        {
            if (parameters == null) { throw new ArgumentNullException("parameters"); }
            if (parameters.CultureInfo == null) { throw new ArgumentException("parameters.CultureInfo is required", "parameters"); }

            var pagesConfiguration = SiteConfiguration.GetPagesConfiguration(parameters.CultureInfo, ComposerContext.WebsiteId);
            var signInPath = PageService.GetPageUrl(pagesConfiguration.CheckoutSignInPageId, parameters.CultureInfo);

            if (string.IsNullOrWhiteSpace(parameters.ReturnUrl))
            {
                return signInPath;
            }

            var urlBuilder = new UrlBuilder(signInPath);
            urlBuilder["ReturnUrl"] = GetReturnUrl(parameters); // url builder will encode the query string value

            return urlBuilder.ToString();
        }

        private static string GetReturnUrl(GetCartUrlParam parameters)
        {
            var returnUrl = Uri.IsWellFormedUriString(parameters.ReturnUrl, UriKind.Relative)
                ? new Uri(parameters.ReturnUrl, UriKind.Relative)
                : new Uri(parameters.ReturnUrl);
            
            return returnUrl.ToString();
        }

        public string GetCheckoutStepUrl(GetCheckoutStepUrlParam parameters)
        {
            var stepUrls = GetCheckoutStepPageInfos(new GetCartUrlParam
            {                
                CultureInfo = parameters.CultureInfo
            });

            if (!stepUrls.ContainsKey(parameters.StepNumber))
            {
                throw new ArgumentOutOfRangeException("parameters", "StepNumber is invalid");
            }

            return stepUrls[parameters.StepNumber].Url;
        }

        public Dictionary<int, CheckoutStepPageInfo> GetCheckoutStepPageInfos(GetCartUrlParam parameters)
        {
            CacheKey cacheKey = new CacheKey(CacheConfigurationCategoryNames.CheckoutStepUrls)
            {
                CultureInfo = parameters.CultureInfo
            };
            Dictionary<int, CheckoutStepPageInfo> stepUrls = CacheProvider.Get< Dictionary<int, CheckoutStepPageInfo>>(cacheKey);

            if (stepUrls != null)
                return stepUrls;

            stepUrls = new Dictionary<int, CheckoutStepPageInfo>();

            var items = PageService.GetCheckoutStepPages(parameters.CultureInfo);

            foreach (var checkoutStepItem in items)
            {
                stepUrls.Add(checkoutStepItem.CurrentStep, new CheckoutStepPageInfo
                {
                    Url = PageService.GetPageUrl(checkoutStepItem.PageId, parameters.CultureInfo),
                    IsDisplayedInHeader = checkoutStepItem.IsDisplayedInHeader,
                    Title = PageService.GetPage(checkoutStepItem.PageId, parameters.CultureInfo).MenuTitle
                });
            }

            stepUrls = stepUrls.OrderBy(x => x.Key).ToDictionary(x => x.Key, y => y.Value);

            CacheProvider.Set(cacheKey, stepUrls);

            return stepUrls;
        }

        public string GetCheckoutAddAddressUrl(GetCartUrlParam param)
        {
            if (param == null) { throw new ArgumentNullException("param"); }

            var pagesConfiguration = SiteConfiguration.GetPagesConfiguration(param.CultureInfo, ComposerContext.WebsiteId);
            var url = PageService.GetPageUrl(pagesConfiguration.CheckoutAddAddressPageId, param.CultureInfo);
            return UrlProviderHelper.BuildUrlWithParams(url, param.ReturnUrl);
        }

        public string GetCheckoutUpdateAddressBaseUrl(GetCartUrlParam param)
        {
            if (param == null) { throw new ArgumentNullException("param"); }

            var pagesConfiguration = SiteConfiguration.GetPagesConfiguration(param.CultureInfo, ComposerContext.WebsiteId);
            var url = PageService.GetPageUrl(pagesConfiguration.CheckoutUpdateAddressPageId, param.CultureInfo);
            return UrlProviderHelper.BuildUrlWithParams(url, param.ReturnUrl);
        }

        public string GetHomepageUrl(GetCartUrlParam param)
        {
            if (param == null) { throw new ArgumentNullException("param"); }

             var url = PageService.GetPageUrl(ComposerContext.WebsiteId, param.CultureInfo);
            ///TODO - fix this
            if (string.IsNullOrWhiteSpace(url))
                return url;

            return UrlProviderHelper.BuildUrlWithParams(url, param.ReturnUrl);
        }
    }
}
