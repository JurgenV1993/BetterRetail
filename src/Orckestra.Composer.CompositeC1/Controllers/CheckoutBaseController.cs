using System;
using System.Linq;
using System.Web.Mvc;
using Composite.Data;
using Orckestra.Composer.Cart;
using Orckestra.Composer.Cart.Parameters;
using Orckestra.Composer.Cart.Services;
using Orckestra.Composer.Cart.ViewModels;
using Orckestra.Composer.CompositeC1.Services;
using Orckestra.Composer.MvcFilters;
using Orckestra.Composer.Parameters;
using Orckestra.Composer.Providers;
using Orckestra.Composer.Services;
using Orckestra.Composer.Services.Breadcrumb;
using Orckestra.Composer.Utils;

namespace Orckestra.Composer.CompositeC1.Controllers
{
    [ValidateReturnUrl]
    public abstract class CheckoutBaseController : Controller
    {
        protected IPageService PageService { get; private set; }
        protected IComposerContext ComposerContext { get; private set; }
        protected ICheckoutBreadcrumbViewService ConfirmationBreadcrumbViewService { get; private set; }
        protected IBreadcrumbViewService BreadcrumbViewService { get; private set; }
        protected ILanguageSwitchService LanguageSwitchService { get; private set; }
        protected ICartUrlProvider UrlProvider { get; private set; }
        protected ICheckoutNavigationViewService CheckoutNavigationViewService { get; private set; }
        protected IPaymentViewService PaymentViewService { get; private set; }
        protected IMyAccountUrlProvider MyAccountUrlProvider { get; private set; }
        protected IWebsiteContext WebsiteContext { get; private set; }
        protected ICartService CartService { get; private set; }

        protected CheckoutBaseController(
            IPageService pageService,
            IComposerContext composerContext,
            ICheckoutBreadcrumbViewService confirmationBreadcrumbViewService,
            IBreadcrumbViewService breadcrumbViewService,
            ILanguageSwitchService languageSwitchService,
            ICartUrlProvider urlProvider,
            ICheckoutNavigationViewService checkoutNavigationViewService,
            IPaymentViewService paymentViewService,
            IMyAccountUrlProvider myAccountUrlProvider,
            ICartService cartService,
            IWebsiteContext websiteContext
            )
        {
            PageService = pageService ?? throw new ArgumentNullException(nameof(pageService));
            ComposerContext = composerContext ?? throw new ArgumentNullException(nameof(composerContext));
            ConfirmationBreadcrumbViewService = confirmationBreadcrumbViewService ?? throw new ArgumentNullException(nameof(confirmationBreadcrumbViewService));
            BreadcrumbViewService = breadcrumbViewService ?? throw new ArgumentNullException(nameof(breadcrumbViewService));
            LanguageSwitchService = languageSwitchService ?? throw new ArgumentNullException(nameof(languageSwitchService));
            UrlProvider = urlProvider ?? throw new ArgumentNullException(nameof(urlProvider));
            CheckoutNavigationViewService = checkoutNavigationViewService ?? throw new ArgumentNullException(nameof(checkoutNavigationViewService));
            PaymentViewService = paymentViewService ?? throw new ArgumentNullException(nameof(paymentViewService));
            MyAccountUrlProvider = myAccountUrlProvider ?? throw new ArgumentNullException(nameof(myAccountUrlProvider));
            WebsiteContext = websiteContext;
            CartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
        }

        public virtual ActionResult GuestCustomerInfo()
        {
            return View("GuestCustomerInfoContainer", BuildCartViewModel());
        }

        public virtual ActionResult ShippingAddress()
        {
            return View("ShippingAddressContainer", BuildCartViewModel());
        }

        public virtual ActionResult ShippingMethod()
        {
            return View("ShippingMethodContainer", BuildCartViewModel());
        }

        public virtual ActionResult CheckoutComplete()
        {
            return View("CheckoutCompleteContainer", BuildCartViewModel());
        }

        public virtual ActionResult CheckoutPayment()
        {
            var checkoutPaymentViewModel = new CheckoutPaymentViewModel
            {
                IsLoading = true
            };

            var getPaymentProvidersParam = new GetPaymentProvidersParam
            {
                Scope = ComposerContext.Scope,
                CultureInfo = ComposerContext.CultureInfo
            };

            checkoutPaymentViewModel.Context.Add("PaymentProviders", PaymentViewService.GetPaymentProvidersAsync(getPaymentProvidersParam).Result.ToList());

            return View("CheckoutPaymentContainer", checkoutPaymentViewModel);
        }

        public virtual ActionResult Breadcrumb()
        {
            var breadcrumbViewModel = BreadcrumbViewService.CreateBreadcrumbViewModel(new GetBreadcrumbParam
            {
                CurrentPageId = SitemapNavigator.CurrentPageId.ToString(),
                CultureInfo = ComposerContext.CultureInfo
            });

            return View(breadcrumbViewModel);
        }

        public virtual ActionResult ConfirmationBreadcrumb()
        {
            var breadcrumbViewModel = ConfirmationBreadcrumbViewService.CreateBreadcrumbViewModel(new GetCheckoutBreadcrumbParam
            {
                CultureInfo = ComposerContext.CultureInfo,
                HomeUrl = PageService.GetRendererPageUrl(WebsiteContext.WebsiteId, ComposerContext.CultureInfo),
            });

            return View("Breadcrumb", breadcrumbViewModel);
        }

        public virtual ActionResult LanguageSwitch()
        {
            var pageId = SitemapNavigator.CurrentPageId;

            var languageSwitchViewModel = LanguageSwitchService.GetViewModel(ci => PageService.GetRendererPageUrl(pageId, ci), ComposerContext.CultureInfo);

            return View("LanguageSwitch", languageSwitchViewModel);
        }

        [MustBeAnonymous(MustBeAnonymousAttribute.CartDestination)]
        public virtual ActionResult CheckoutSignInAsGuest()
        {
            var stepOneUrl = UrlProvider.GetCheckoutStepUrl(new GetCheckoutStepUrlParam
            {
                CultureInfo = ComposerContext.CultureInfo,
                StepNumber = 1
            });

            var registerUrl = MyAccountUrlProvider.GetCreateAccountUrl(new BaseUrlParameter
            {
                CultureInfo = ComposerContext.CultureInfo,
                ReturnUrl = stepOneUrl
            });

            var cart = CartService.GetCartViewModelAsync(new GetCartParam()
            {
                BaseUrl = RequestUtils.GetBaseUrl(Request).ToString(),
                CartName = CartConfiguration.ShoppingCartName,
                CultureInfo = ComposerContext.CultureInfo,
                CustomerId = ComposerContext.CustomerId,
                ExecuteWorkflow = true,
                Scope = ComposerContext.Scope
            }).Result;

            var hasRecurringItems = cart.HasRecurringLineitems;

            var checkoutSignInAsGuestViewModel = new CheckoutSignInAsGuestViewModel
            {
                CheckoutUrlTarget = stepOneUrl,
                RegisterUrl = registerUrl,
                IsCartContainsRecurringLineitems = hasRecurringItems
            };

            return View("CheckoutSignInAsGuest", checkoutSignInAsGuestViewModel);
        }

        [MustBeAnonymous(MustBeAnonymousAttribute.CartDestination)]
        public virtual ActionResult CheckoutSignInAsCustomer()
        {
            var forgotPasswordUrl = MyAccountUrlProvider.GetForgotPasswordUrl(new BaseUrlParameter
            {
                CultureInfo = ComposerContext.CultureInfo
            });

            var vm = new CheckoutSignInAsReturningViewModel
            {
                ForgotPasswordUrl = forgotPasswordUrl
            };

            return View("ReturningCustomerBlade", vm);
        }

        public virtual ActionResult CheckoutOrderSummary()
        {
            return View("CheckoutOrderSummaryContainer", BuildCartViewModel());
        }

        public virtual ActionResult CompleteCheckoutOrderSummary()
        {
            return View("CompleteCheckoutOrderSummaryContainer", BuildCartViewModel());
        }

        public virtual ActionResult CheckoutFinalStepOrderSummary()
        {
            var cartViewModel = BuildCartViewModel();

            cartViewModel.Context.Add("RedirectUrl", UrlProvider.GetCartUrl(new BaseUrlParameter
            {
                CultureInfo = ComposerContext.CultureInfo
            }));

            return View("CheckoutOrderConfirmationContainer", BuildCartViewModel());
        }

        public virtual ActionResult BillingAddress()
        {
            return View("BillingAddressContainer", BuildCartViewModel());
        }

        public virtual ActionResult BillingAddressRegistered()
        {
            return View("BillingAddressRegisteredContainer", BuildCartViewModel());
        }

        public virtual ActionResult ShippingAddressRegistered()
        {
            return View("ShippingAddressRegisteredContainer", BuildCartViewModel());
        }

        public virtual ActionResult CheckoutNavigation()
        {
            var navigationPageInfos = UrlProvider.GetCheckoutStepPageInfos(new BaseUrlParameter
            {
                CultureInfo = ComposerContext.CultureInfo
            });

            var currentStep = PageService.GetCheckoutStepPageNumber(SitemapNavigator.CurrentHomePageId, SitemapNavigator.CurrentPageId, ComposerContext.CultureInfo);

            var checkoutNavigationViewModel = CheckoutNavigationViewService.GetCheckoutNavigationViewModel(new GetCheckoutNavigationParam
            {
                StepUrls = navigationPageInfos,
                CurrentStep = currentStep
            });

            return View("CheckoutNavigation", checkoutNavigationViewModel);
        }

        protected virtual CartViewModel BuildCartViewModel()
        {
            var cartViewModel = new CartViewModel
            {
                GettingCart = true,
                IsAuthenticated = ComposerContext.IsAuthenticated
            };

            var currentStep = PageService.GetCheckoutStepPageNumber(SitemapNavigator.CurrentHomePageId, SitemapNavigator.CurrentPageId, ComposerContext.CultureInfo);

            cartViewModel.Context.Add("CurrentStep", currentStep);

            return cartViewModel;
        }
    }
}
