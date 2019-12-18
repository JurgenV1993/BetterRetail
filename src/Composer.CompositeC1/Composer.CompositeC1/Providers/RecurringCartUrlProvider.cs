﻿using Orckestra.Composer.CompositeC1.Services;
using Orckestra.Composer.Configuration;
using Orckestra.Composer.Parameters;
using Orckestra.Composer.Providers;
using Orckestra.Composer.Utils;
using System;
using System.Collections.Specialized;

namespace Orckestra.Composer.CompositeC1.Providers
{
    public class RecurringCartUrlProvider : IRecurringCartUrlProvider
    {
        protected IPageService PageService { get; private set; }
    
        protected IRecurringOrdersSettings RecurringOrdersSettings { get; private set; }

        public RecurringCartUrlProvider(IPageService pageService, IRecurringOrdersSettings recurringOrdersSettings)
        {
            if (pageService == null) { throw new ArgumentNullException("pageService"); }

            PageService = pageService;
       
            RecurringOrdersSettings = recurringOrdersSettings;

        }
        public string GetRecurringCartsUrl(GetRecurringCartsUrlParam param)
        {
            if (param == null) { throw new ArgumentNullException(nameof(param)); }

            var url = PageService.GetPageUrl(RecurringOrdersSettings.RecurringCartsPageId, param.CultureInfo);
            return UrlProviderHelper.BuildUrlWithParams(url, param.ReturnUrl);
        }

        public string GetRecurringCartDetailsUrl(GetRecurringCartDetailsUrlParam param)
        {
            if (param == null) { throw new ArgumentNullException(nameof(param)); }
            if (param.RecurringCartName == null) { throw new ArgumentNullException(nameof(param.RecurringCartName)); }

            var url = PageService.GetPageUrl(RecurringOrdersSettings.RecurringCartDetailsPageId, param.CultureInfo);
            var UrlWithReturn = UrlProviderHelper.BuildUrlWithParams(url, param.ReturnUrl);

            return UrlFormatter.AppendQueryString(UrlWithReturn, new NameValueCollection
                {
                    {"name", param.RecurringCartName }
                });
        }
    }
}
