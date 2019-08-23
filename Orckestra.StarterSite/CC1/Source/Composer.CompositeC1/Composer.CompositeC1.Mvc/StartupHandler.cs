﻿using Composite.AspNet.MvcFunctions;
using Composite.Core.Application;
using Composite.Core.Xml;
using Composite.Data.DynamicTypes;
using Composite.Functions;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using Orckestra.Composer.CompositeC1.DataTypes;
using Orckestra.Composer.CompositeC1.DataTypes.Navigation;
using Orckestra.Composer.CompositeC1.Hangfire;
using Orckestra.Composer.CompositeC1.Mvc.Controllers;
using Orckestra.Composer.CompositeC1.Pages;
using Orckestra.Composer.CompositeC1.Sitemap;
using Orckestra.Composer.HttpModules;
using Orckestra.Composer.Logging;
using Orckestra.Composer.Search;
using System;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using Composite.Data;
using ExperienceManagement.DataTypes;

namespace Orckestra.Composer.CompositeC1.Mvc
{
    [ApplicationStartup(AbortStartupOnException = true)]
    public static class StartupHandler
    {
        public static void Start()
        {
            DynamicModuleUtility.RegisterModule(typeof(SecurityModule));
            SetUpSearchConfiguration();
        }

        public static void OnBeforeInitialize()
        {

        }

        public static void OnInitialized()
        {
            LogProvider.SetCurrentLogProvider(C1LogProvider.Instance);

            // Needed by C1, do not remove!
            var host = new ComposerHost();
            host.Init();

            var log = LogProvider.GetCurrentClassLogger();

            log.Info("Application Starting");

            DynamicTypeManager.EnsureCreateStore(typeof(ComposerPage));
            DynamicTypeManager.EnsureCreateStore(typeof(CategoryPage));
            DynamicTypeManager.EnsureCreateStore(typeof(ComposerImage));
            DynamicTypeManager.EnsureCreateStore(typeof(CheckoutStepInfoPage));
            DynamicTypeManager.EnsureCreateStore(typeof(UrlTarget));
            DynamicTypeManager.EnsureCreateStore(typeof(CssStyle));
            DynamicTypeManager.EnsureCreateStore(typeof(MainMenu));
            DynamicTypeManager.EnsureCreateStore(typeof(StickyHeader));
            DynamicTypeManager.EnsureCreateStore(typeof(HeaderOptionalLink));
            DynamicTypeManager.EnsureCreateStore(typeof(NavigationImage));
            DynamicTypeManager.EnsureCreateStore(typeof(Footer));
            DynamicTypeManager.EnsureCreateStore(typeof(FooterOptionalLink));

            var functions = MvcFunctionRegistry.NewFunctionCollection();
            RegisterFunctions(functions);
            RegisterFunctionRoutes(functions);

            log.Info("Application Started");

            if (HangfireHost.IsEnabled)
            {
                // Hangfire host and sitemap recurring job
                HangfireHost.Current.Init(new SitemapAutofacModule());
                HangfireHost.Current.RegisterRecurringJobIfScheduleIsDefined();
            }
            else
            {
                log.Info("Hangfire automatic start explicitly disabled via app setting - hangfire will not run.");
            }
        }

        private static void RegisterFunctions(FunctionCollection functions)
        {
            functions.AutoDiscoverFunctions(typeof(StartupHandler).Assembly);

            functions.RegisterAction<HeaderController>("GeneralErrors", "Composer.Header.GeneralErrors");
            functions.RegisterAction<HeaderController>("LanguageSwitch", "Composer.Header.LanguageSwitch");
            functions.RegisterAction<HeaderController>("Breadcrumb", "Composer.General.Breadcrumb");
            functions.RegisterAction<HeaderController>("MainMenu", "Composer.Header.MainMenu");
            functions.RegisterAction<HeaderController>("StickyLinks", "Composer.Header.StickyLinks");
            functions.RegisterAction<HeaderController>("OptionalLinks", "Composer.Header.OptionalLinks");
            functions.RegisterAction<HeaderController>("PageHeader", "Composer.Header.PageHeader");

            functions.RegisterAction<FooterController>("OptionalLinks", "Composer.Footer.OptionalLinks");
            functions.RegisterAction<FooterController>("SocialLinks", "Composer.Footer.SocialLinks");
            functions.RegisterAction<FooterController>("Copyright", "Composer.Footer.Copyright");
            functions.RegisterAction<FooterController>("MainFooter", "Composer.Footer.MainFooter");

            functions.RegisterAction<SearchController>("PageHeader", "Composer.Search.PageHeader");
            functions.RegisterAction<SearchController>("SearchBox", "Composer.SearchBox");
            functions.RegisterAction<SearchController>("Index", "Composer.Search.Index");
            functions.RegisterAction<SearchController>("SearchFacets", "Composer.Search.Facets");
            functions.RegisterAction<SearchController>("SearchSummary", "Composer.Search.Summary");
            functions.RegisterAction<SearchController>("Breadcrumb", "Composer.Search.Breadcrumb");
            functions.RegisterAction<SearchController>("LanguageSwitch", "Composer.Search.LanguageSwitch");
            functions.RegisterAction<SearchController>("SelectedSearchFacets", "Composer.Search.SelectedSearchFacets");

            functions.RegisterAction<BrowsingCategoriesController>("LanguageSwitch", "Composer.BrowsingCategories.LanguageSwitch");
            functions.RegisterAction<BrowsingCategoriesController>("Summary", "Composer.BrowsingCategories.Summary");
            functions.RegisterAction<BrowsingCategoriesController>("Index", "Composer.BrowsingCategories.Results");
            functions.RegisterAction<BrowsingCategoriesController>("Facets", "Composer.BrowsingCategories.Facets");
            functions.RegisterAction<BrowsingCategoriesController>("SelectedSearchFacets", "Composer.BrowsingCategories.SelectedFacets");
            functions.RegisterAction<BrowsingCategoriesController>("ChildCategories", "Composer.BrowsingCategories.ChildCategories");

            functions.RegisterAction<ProductController>("PageHeader", "Composer.Product.PageHeader");
            functions.RegisterAction<ProductController>("LanguageSwitch", "Composer.Product.LanguageSwitch");
            functions.RegisterAction<ProductController>("ProductSummary", "Composer.Product.Summary");
            functions.RegisterAction<ProductController>("ProductDescription", "Composer.Product.Description");
            functions.RegisterAction<ProductController>("ProductSpecifications", "Composer.Product.Specifications");
            functions.RegisterAction<ProductController>("Breadcrumb", "Composer.Product.Breadcrumb");
            functions.RegisterAction<ProductController>("RelatedProducts", "Composer.Product.RelatedProducts", "Displays products/variants related to the product displayed on the current product/variant details page.  First products which are related via merchandising relationship will be displayed and if none are available then displays product in the same default category")
                .AddParameter("merchandiseTypes", 
                    typeof(string),
                    label: "Products Merchandise Relationship Types to include",
                    helpText: "Specify the Merchandise Types ")
                .AddParameter(
                    "headingText", 
                    typeof(string),
                    defaultValueProvider: new ConstantValueProvider("You may also like"),
                    label: "Heading",
                    helpText: "Displays the header of the related products block. The header must be short. Use text like \"You might also like\".")
                .AddParameter(
                    "displaySameCategoryProducts",
                    typeof(bool),
                    defaultValueProvider: new ConstantValueProvider(true),
                    label: "Display products in the same category",
                    helpText: "Specify if this block should display products in the same default category if no products are displayed based on specified relationship types.")
                .AddParameter(
                    "maxItems",
                    typeof(int), 
                    defaultValueProvider: new ConstantValueProvider(5),
                    label: "Number of maximum displayed products/variants",
                    helpText: "Specify the number of products/items displayed in this block. The maximum should be 15.")
                .AddParameter(
                    "displayPrices",
                    typeof(bool),
                    defaultValueProvider: new ConstantValueProvider(true),
                    label: "Display price on products",
                    helpText: "Show the price on the products in this block.")
                .AddParameter(
                    "displayAddToCart",
                    typeof(bool),
                    defaultValueProvider: new ConstantValueProvider(true),
                    label: "Display \"Add to cart\" on products",
                    helpText: "Show the \"Add to cart\" link on the products in this block.")
                .AddParameter(
                    "backgroundStyle",
                    typeof(DataReference<CssStyle>),
                    label: "Background style",
                    helpText: "Specify the style of this block background");

            functions.RegisterAction<CartController>("CartSummary", "Composer.Cart.CartSummary");
            functions.RegisterAction<CartController>("OrderSummary", "Composer.Cart.OrderSummary");
            functions.RegisterAction<CartController>("Coupons", "Composer.Cart.Coupons");

            functions.RegisterAction<CartController>("Minicart", "Composer.Cart.Minicart").AddParameter(
                "notificationTimeInSeconds",
                typeof(int),
                isRequired: true,
                defaultValueProvider: new ConstantValueProvider(5),
                label: "Minicart notification time",
                helpText: "Notification time of the minicart when an item is added/updated in cart (in seconds)."
            );

            functions.RegisterAction<CheckoutController>("GuestCustomerInfo", "Composer.Checkout.GuestCustomerInfo");
            functions.RegisterAction<CheckoutController>("ShippingAddress", "Composer.Checkout.ShippingAddress");
            functions.RegisterAction<CheckoutController>("ShippingMethod", "Composer.Checkout.ShippingMethod");
            functions.RegisterAction<CheckoutController>("CheckoutSignInAsGuest", "Composer.Checkout.CheckoutSignInAsGuest");
            functions.RegisterAction<CheckoutController>("CheckoutSignInAsCustomer", "Composer.Checkout.CheckoutSignInAsCustomer");
            functions.RegisterAction<CheckoutController>("CheckoutOrderSummary", "Composer.Checkout.CheckoutOrderSummary");
            functions.RegisterAction<CheckoutController>("CheckoutFinalStepOrderSummary", "Composer.Checkout.CheckoutFinalStepOrderSummary");
            functions.RegisterAction<CheckoutController>("CheckoutComplete", "Composer.Checkout.CheckoutComplete");
            functions.RegisterAction<CheckoutController>("ShippingAddressRegistered", "Composer.Checkout.ShippingAddressRegistered");
            functions.RegisterAction<CheckoutController>("BillingAddress", "Composer.Checkout.BillingAddress");
            functions.RegisterAction<CheckoutController>("BillingAddressRegistered", "Composer.Checkout.BillingAddressRegistered");
            functions.RegisterAction<CheckoutController>("Breadcrumb", "Composer.Checkout.Breadcrumb");
            functions.RegisterAction<CheckoutController>("ConfirmationBreadcrumb", "Composer.Checkout.ConfirmationBreadcrumb");
            functions.RegisterAction<CheckoutController>("LanguageSwitch", "Composer.Checkout.LanguageSwitch");
            functions.RegisterAction<CheckoutController>("CompleteCheckoutOrderSummary", "Composer.Checkout.CompleteCheckoutOrderSummary");
            functions.RegisterAction<CheckoutController>("CheckoutPayment", "Composer.Checkout.CheckoutPayment");
            functions.RegisterAction<CheckoutController>("CheckoutNavigation", "Composer.Checkout.CheckoutNavigation");

            functions.RegisterAction<MembershipController>("SignInHeaderBlade", "Composer.Membership.SignInHeader");
            functions.RegisterAction<MembershipController>("ReturningCustomerBlade", "Composer.Membership.ReturningCustomer");
            functions.RegisterAction<MembershipController>("NewCustomerBlade", "Composer.Membership.NewCustomer");
            functions.RegisterAction<MembershipController>("CreateAccountBlade", "Composer.Membership.CreateAccount");
            functions.RegisterAction<MembershipController>("ForgotPasswordBlade", "Composer.Membership.ForgotPassword");
            functions.RegisterAction<MembershipController>("NewPasswordBlade", "Composer.Membership.NewPassword");
            functions.RegisterAction<MembershipController>("ChangePasswordBlade", "Composer.Membership.ChangePassword");

            functions.RegisterAction<MyAccountController>("AccountHeader", "Composer.MyAccount.AccountHeader");
            functions.RegisterAction<MyAccountController>("UpdateAccount", "Composer.MyAccount.UpdateAccount");
            functions.RegisterAction<MyAccountController>("AddressList", "Composer.MyAccount.AddressList");
            functions.RegisterAction<MyAccountController>("CreateAddress", "Composer.MyAccount.CreateAddress");
            functions.RegisterAction<MyAccountController>("EditAddress", "Composer.MyAccount.UpdateAddress").IncludePathInfo();
            functions.RegisterAction<MyAccountController>("MyAccountMenu", "Composer.MyAccount.MyAccountMenu");
            functions.RegisterAction<MyAccountController>("CurrentOrders", "Composer.MyAccount.CurrentOrders");
            functions.RegisterAction<MyAccountController>("PastOrders", "Composer.MyAccount.PastOrders");
            functions.RegisterAction<MyAccountController>("OrderDetails", "Composer.MyAccount.OrderDetails");
            functions.RegisterAction<MyAccountController>("WishList", "Composer.MyAccount.WishList")
                .AddParameter("emptyWishListContent", typeof(XhtmlDocument), true, label: "Empty Wish List Content", helpText: "That content will be shown when Wish List is Empty");
            functions.RegisterAction<MyAccountController>("RecurringSchedule", "Composer.MyAccount.RecurringSchedule")
                .AddParameter("emptyRecurringScheduleContent", typeof(XhtmlDocument), true, label: "Empty Recurring Schedule Content", helpText: "That content will be shown when Recurring Schedule is Empty");
            functions.RegisterAction<MyAccountController>("RecurringScheduleDetails", "Composer.MyAccount.RecurringScheduleDetails");
            functions.RegisterAction<MyAccountController>("UpcomingOrders", "Composer.MyAccount.UpcomingOrders");
            functions.RegisterAction<MyAccountController>("RecurringCartDetails", "Composer.MyAccount.RecurringCartDetails");

            functions.RegisterAction<WishListController>("WishListInHeader", "Composer.WishList.WishListInHeader");
            functions.RegisterAction<WishListController>("SharedWishList", "Composer.WishList.Shared")
                .AddParameter("emptyWishListContent", typeof(XhtmlDocument), true, label: "Empty Wish List Content", helpText: "That content will be shown when Wish List is Empty"); ;
            functions.RegisterAction<WishListController>("SharedWishListTitle", "Composer.WishList.SharedTitle");

            functions.RegisterAction<OrderController>("FindMyOrder", "Composer.Order.FindMyOrder");
            functions.RegisterAction<OrderController>("OrderDetails", "Composer.Order.OrderDetails");

            functions.RegisterAction<StoreLocatorController>("Index", "Composer.Store.Locator")
                .AddParameter("pagesize", typeof(int), false, label: "Page Size", helpText: "The max count of the items to show in the list.");
            functions.RegisterAction<StoreLocatorController>("StoreDetails", "Composer.Store.Details", "Store Details")
                .AddParameter("zoom", typeof(int), false, label: "Map Zoom Level", helpText: "Define the resolution of the map view. Zoom levels between 0 and 21+. Default is 14 (streets).");
            functions.RegisterAction<StoreLocatorController>("Breadcrumb", "Composer.Store.Breadcrumb");
            functions.RegisterAction<StoreLocatorController>("PageHeader", "Composer.Store.PageHeader");
            functions.RegisterAction<StoreLocatorController>("StoreDirectory", "Composer.Store.Directory");
            functions.RegisterAction<StoreLocatorController>("StoreLocatorInHeader", "Composer.Store.LocatorInHeader");
            functions.RegisterAction<StoreLocatorController>("StoreInventory", "Composer.Store.Inventory")
                .AddParameter("pagesize", typeof(int), false, label: "Page Size", helpText: "The max count of the items to show in the list.");
            functions.RegisterAction<StoreLocatorController>("LanguageSwitch", "Composer.StoreLocator.LanguageSwitch");
            functions.RegisterAction<PageNotFoundController>("PageNotFoundAnalytics", "Composer.PageNotFound.Analytics");
        }

        private static void RegisterFunctionRoutes(FunctionCollection functions)
        {
            functions.RouteCollection.MapMvcAttributeRoutes();
            functions.RouteCollection.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { action = "Index", id = UrlParameter.Optional } // Parameter defaults
                );
        }

        private static void SetUpSearchConfiguration()
        {
            SearchConfiguration.ShowAllPages = true;
        }
    }
}