﻿using System;
using Orckestra.Composer.CompositeC1.Controllers;
using Orckestra.Composer.CompositeC1.Services;
using Orckestra.Composer.CompositeC1.Services.PreviewMode;
using Orckestra.Composer.Product.Services;
using Orckestra.Composer.Providers;
using Orckestra.Composer.Services;

namespace Orckestra.Composer.CompositeC1.Mvc.Controllers
{
    public class ProductController : ProductBaseController
    {
        public ProductController(
            IPageService pageService, 
            IComposerContext composerContext, 
            IProductViewService productService, 
            IProductSpecificationsViewService productSpecificationsViewService, 
            IProductBreadcrumbService productBreadcrumbService, 
            ILanguageSwitchService languageSwitchService, 
            IProductUrlProvider productUrlProvider, 
            IRelatedProductViewService relatedProductViewService,
            Lazy<IPreviewModeService> previewModeService) 
            
            : base(
            pageService, 
            composerContext, 
            productService, 
            productSpecificationsViewService, 
            productBreadcrumbService, 
            languageSwitchService, 
            productUrlProvider, 
            relatedProductViewService,
            previewModeService)
        {
        }
    }
}