﻿using System;
using System.Globalization;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Composite.Data;
using Orckestra.Composer.CompositeC1.DataTypes;
using Orckestra.Composer.CompositeC1.Extensions;
using Orckestra.Composer.CompositeC1.Services;
using Orckestra.Composer.CompositeC1.Services.PreviewMode;
using Orckestra.Composer.Parameters;
using Orckestra.Composer.Product.Parameters;
using Orckestra.Composer.Product.Services;
using Orckestra.Composer.Product.ViewModels;
using Orckestra.Composer.Providers;
using Orckestra.Composer.Services;
using Orckestra.Composer.Utils;

namespace Orckestra.Composer.CompositeC1.Controllers
{
    public abstract class ProductBaseController : Controller
    {
        protected IPageService PageService { get; private set; }
        protected IComposerContext ComposerContext { get; private set; }
        protected IProductViewService ProductService { get; private set; }
        protected IProductSpecificationsViewService ProductSpecificationsViewService { get; private set; }
        protected IProductBreadcrumbService ProductBreadcrumbService { get; private set; }
        protected ILanguageSwitchService LanguageSwitchService { get; private set; }
        protected IProductUrlProvider ProductUrlProvider { get; private set; }
        protected IRelatedProductViewService RelatedProductViewService { get; private set; }
        protected Lazy<IPreviewModeService> PreviewModeService { get; }

        protected ProductBaseController(
            IPageService pageService,
            IComposerContext composerContext,
            IProductViewService productService,
            IProductSpecificationsViewService productSpecificationsViewService,
            IProductBreadcrumbService productBreadcrumbService,
            ILanguageSwitchService languageSwitchService,
            IProductUrlProvider productUrlProvider,
            IRelatedProductViewService relatedProductViewService,
            Lazy<IPreviewModeService> previewModeService)
        {
            PageService = pageService ?? throw new ArgumentNullException(nameof(pageService));
            ComposerContext = composerContext ?? throw new ArgumentNullException(nameof(composerContext));
            ProductService = productService ?? throw new ArgumentNullException(nameof(productService));
            ProductSpecificationsViewService = productSpecificationsViewService ?? throw new ArgumentNullException(nameof(productSpecificationsViewService));
            ProductBreadcrumbService = productBreadcrumbService ?? throw new ArgumentNullException(nameof(productBreadcrumbService));
            LanguageSwitchService = languageSwitchService ?? throw new ArgumentNullException(nameof(languageSwitchService));
            ProductUrlProvider = productUrlProvider ?? throw new ArgumentNullException(nameof(productUrlProvider));
            RelatedProductViewService = relatedProductViewService ?? throw new ArgumentNullException(nameof(relatedProductViewService));
            PreviewModeService = previewModeService ?? throw new ArgumentNullException(nameof(previewModeService));
        }

        public virtual ActionResult ProductSummary(string id, string variantId)
        {
            return GetProductDetail(id, variantId);
        }

        public virtual ActionResult ProductDescription(string id, string variantId)
        {
            return GetProductDetail(id, variantId);
        }

        public virtual ActionResult ProductSEO(string id, string variantId)
        {
            return GetProductDetail(id, variantId);
        }

        public virtual ActionResult LanguageSwitch(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return this.HandlePreviewMode(() => LanguageSwitch(PreviewModeService.Value.GetProductId()));
            }

            var productViewModel = ProductService.GetProductViewModelAsync(new GetProductParam
            {
                ProductId = id,
                CultureInfo = ComposerContext.CultureInfo,
                Scope = ComposerContext.Scope,
                BaseUrl = RequestUtils.GetBaseUrl(Request).ToString()
            }).Result;

            if (productViewModel == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            var languageSwitchViewModel = LanguageSwitchService.GetViewModel(cultureInfo => BuildUrl(
                cultureInfo,
                productViewModel.LocalizedDisplayNames[cultureInfo.Name],
                productViewModel.ProductId,
                productViewModel.SelectedVariantId),
                ComposerContext.CultureInfo);

            return View("LanguageSwitch", languageSwitchViewModel);
        }

        private string BuildUrl(CultureInfo cultureInfo, string productName, string productId, string variantId)
        {
            var productUrl = ProductUrlProvider.GetProductUrl(new GetProductUrlParam
            {                
                CultureInfo = cultureInfo,
                ProductId = productId,
                ProductName = productName,
                VariantId = variantId
            });

            return productUrl;
        }

        public virtual ActionResult Breadcrumb(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return this.HandlePreviewMode(() => Breadcrumb(PreviewModeService.Value.GetProductId()));
            }

            var productViewModel = ProductService.GetProductViewModelAsync(new GetProductParam
            {
                ProductId = id,
                CultureInfo = ComposerContext.CultureInfo,
                Scope = ComposerContext.Scope,
                BaseUrl = RequestUtils.GetBaseUrl(Request).ToString()
            }).Result;

            if (productViewModel == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            var parameters = new GetProductBreadcrumbParam
            {
                CategoryId = productViewModel.CategoryId,
                CultureInfo = ComposerContext.CultureInfo,
                HomeUrl = PageService.GetRendererPageUrl(SitemapNavigator.CurrentHomePageId, ComposerContext.CultureInfo),
                ProductName = productViewModel.DisplayName,
                Scope = ComposerContext.Scope,
                BaseUrl = RequestUtils.GetBaseUrl(Request).ToString()
            };

            var breadcrumbViewModel = ProductBreadcrumbService.CreateBreadcrumbAsync(parameters).Result;

            return View(breadcrumbViewModel);
        }

        public virtual ActionResult PageHeader(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return this.HandlePreviewMode(() => PageHeader(PreviewModeService.Value.GetProductId()));
            }

            var vm = ProductService.GetPageHeaderViewModelAsync(new GetPageHeaderParam
            {
                ProductId = id,
                CultureInfo = ComposerContext.CultureInfo,
                Scope = ComposerContext.Scope,
                BaseUrl = RequestUtils.GetBaseUrl(Request).ToString()

            }).Result;

            if (vm == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            return View(vm);
        }

        private ActionResult GetProductDetail(string id, string variantId)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return this.HandlePreviewMode(() => GetProductDetail(PreviewModeService.Value.GetProductId(), string.Empty));
            }
            var productViewModel = ProductService.GetProductViewModelAsync(new GetProductParam
            {
                ProductId = id,
                CultureInfo = ComposerContext.CultureInfo,
                Scope = ComposerContext.Scope,
                VariantId = variantId,
                BaseUrl = RequestUtils.GetBaseUrl(Request).ToString()
            }).Result;

            if (productViewModel == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            return View(productViewModel);
        }

        public virtual ActionResult ProductSpecifications(string id, string variantId)
        {
            if (string.IsNullOrEmpty(id))
            {
                return View();
            }
            var emptySpecificationsViewModel = ProductSpecificationsViewService.GetEmptySpecificationsViewModel(new GetProductSpecificationsParam
            {
                ProductId = id,
                VariantId = variantId
            });

            return View(emptySpecificationsViewModel);
        }

        public virtual ActionResult RelatedProducts(string id, string merchandiseTypes, string headingText, int maxItems, bool displaySameCategoryProducts, bool displayPrices, bool displayAddToCart, DataReference<CssStyle> backgroundStyle=null)
        {
            if (string.IsNullOrEmpty(id))
            {
                return this.HandlePreviewMode( () =>
                            RelatedProducts(PreviewModeService.Value.GetProductId(), merchandiseTypes, headingText, maxItems,
                                displaySameCategoryProducts, displayPrices, displayAddToCart));
            }
            
            if (string.IsNullOrWhiteSpace(merchandiseTypes))
            {
                throw new HttpException(400, "merchandiseTypes parameter is required.");
            }

            var cssStyle = backgroundStyle?.Data?.CssCode;

            var relatedProductsViewModel = CreateRelatedProductsViewModel(id, 
                merchandiseTypes, 
                headingText, 
                maxItems, 
                displaySameCategoryProducts, 
                displayPrices, 
                displayAddToCart,
                cssStyle);

            return View("RelatedProductsBlade", relatedProductsViewModel);
        }

        private RelatedProductsViewModel CreateRelatedProductsViewModel(string id, string merchandiseTypes,
            string headingText, int maxItems, bool displaySameCategoryProducts, bool displayPrices,
            bool displayAddToCart, string backgroundStyle)
        {
            var param = new GetProductIdentifiersParam
            {
                CultureInfo = ComposerContext.CultureInfo,
                ProductId = id,
                Scope = ComposerContext.Scope,
                MerchandiseTypes = merchandiseTypes.Split(','),
                FallbackOnSameCategoriesProduct = displaySameCategoryProducts,
                MaxItems = maxItems
            };

            var relatedProductsViewModel = RelatedProductViewService.GetProductIdsAsync(param).Result;
            relatedProductsViewModel.Context["DisplayAddToCart"] = displayAddToCart;
            relatedProductsViewModel.Context["DisplayPrices"] = displayPrices;
            relatedProductsViewModel.Context["HeadingComponentText"] = headingText;

            if (!string.IsNullOrWhiteSpace(backgroundStyle))
            {
                relatedProductsViewModel.Context["BackgroundStyle"] = backgroundStyle;
            }

            return relatedProductsViewModel; ;
        }
    }
}
