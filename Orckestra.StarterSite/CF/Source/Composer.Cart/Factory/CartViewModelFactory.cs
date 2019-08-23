﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Orckestra.Composer.Cart.Extensions;
using Orckestra.Composer.Cart.Parameters;
using Orckestra.Composer.Cart.Repositories;
using Orckestra.Composer.Cart.ViewModels;
using Orckestra.Composer.Country;
using Orckestra.Composer.Providers;
using Orckestra.Composer.Providers.Localization;
using Orckestra.Composer.Services;
using Orckestra.Composer.ViewModels;
using Orckestra.Overture.ServiceModel;
using Orckestra.Overture.ServiceModel.Marketing;
using Orckestra.Overture.ServiceModel.Orders;
using Orckestra.Composer.Cart.ViewModels.Order;
using Orckestra.Composer.Providers.Dam;

namespace Orckestra.Composer.Cart.Factory
{
    public class CartViewModelFactory : ICartViewModelFactory
    {

        private RetrieveCountryParam _countryParam;
        protected RetrieveCountryParam CountryParam
        {
            get
            {
                return _countryParam ?? (_countryParam = new RetrieveCountryParam
                {
                    CultureInfo = ComposerContext.CultureInfo,
                    IsoCode = ComposerContext.CountryCode
                });
            }
        }

        protected ILocalizationProvider LocalizationProvider { get; private set; }
        protected IViewModelMapper ViewModelMapper { get; private set; }
        protected IFulfillmentMethodRepository FulfillmentMethodRepository { get; private set; }
        protected ICountryService CountryService { get; private set; }
        protected IComposerContext ComposerContext { get; private set; }
        protected ITaxViewModelFactory TaxViewModelFactory { get; private set; }
        protected ILineItemViewModelFactory LineItemViewModelFactory { get; private set; }
        protected IRewardViewModelFactory RewardViewModelFactory { get; private set; }

        public CartViewModelFactory(
            ILocalizationProvider localizationProvider,
            IViewModelMapper viewModelMapper,
            IFulfillmentMethodRepository fulfillmentMethodRepository,
            ICountryService countryService,
            IComposerContext composerContext,
            ITaxViewModelFactory taxViewModelFactory,
            ILineItemViewModelFactory lineItemViewModelFactory,
            IRewardViewModelFactory rewardViewModelFactory)
        {
            if (localizationProvider == null) { throw new ArgumentNullException("localizationProvider"); }
            if (viewModelMapper == null) { throw new ArgumentNullException("viewModelMapper"); }
            if (fulfillmentMethodRepository == null) { throw new ArgumentNullException("fulfillmentMethodRepository"); }
            if (countryService == null) { throw new ArgumentNullException("countryService"); }
            if (taxViewModelFactory == null) { throw new ArgumentNullException("taxViewModelFactory"); }
            if (lineItemViewModelFactory == null) { throw new ArgumentNullException("lineItemViewModelFactory"); }
            if (rewardViewModelFactory == null) { throw new ArgumentNullException("rewardViewModelFactory"); }

            LocalizationProvider = localizationProvider;
            ViewModelMapper = viewModelMapper;
            FulfillmentMethodRepository = fulfillmentMethodRepository;
            CountryService = countryService;
            ComposerContext = composerContext;
            TaxViewModelFactory = taxViewModelFactory;
            LineItemViewModelFactory = lineItemViewModelFactory;
            RewardViewModelFactory = rewardViewModelFactory;
        }

        public virtual CartViewModel CreateCartViewModel(CreateCartViewModelParam param)
        {
            if (param == null) { throw new ArgumentNullException("param"); }
            if (param.CultureInfo == null) { throw new ArgumentNullException("CultureInfo"); }
            if (param.ProductImageInfo == null) { throw new ArgumentNullException("ProductImageInfo"); }
            if (param.ProductImageInfo.ImageUrls == null) { throw new ArgumentNullException("ImageUrls"); }
            if (string.IsNullOrWhiteSpace(param.BaseUrl)) { throw new ArgumentException("BaseUrl"); }

            var vm = ViewModelMapper.MapTo<CartViewModel>(param.Cart, param.CultureInfo);

            if (vm == null) { return null; }

            vm.OrderSummary = GetOrderSummaryViewModel(param.Cart, param.CultureInfo);
            MapShipmentsAndPayments(param.Cart, param.CultureInfo, param.ProductImageInfo, param.BaseUrl, param.PaymentMethodDisplayNames, vm, param.Cart.Payments);
            MapCustomer(param.Cart.Customer, param.CultureInfo, vm);
            vm.Coupons = GetCouponsViewModel(param.Cart, param.CultureInfo, param.IncludeInvalidCouponsMessages);
            vm.OrderSummary.AdditionalFeeSummaryList = GetAdditionalFeesSummary(vm.LineItemDetailViewModels, param.CultureInfo);

            SetDefaultCountryCode(vm);
            SetPostalCodeRegexPattern(vm);
            SetPhoneNumberRegexPattern(vm);

            // Reverse the items order in the Cart so the last added item will be the first in the list
            if (vm.LineItemDetailViewModels != null)
            {
                vm.LineItemDetailViewModels.Reverse();
            }

            vm.IsAuthenticated = ComposerContext.IsAuthenticated;

            return vm;
        }

        //TODO: Remove this once we support the notion of countries other than Canada and also have a country picker
        protected virtual void SetDefaultCountryCode(CartViewModel vm)
        {
            if (vm.ShippingAddress != null && string.IsNullOrWhiteSpace(vm.ShippingAddress.CountryCode))
            {
                vm.ShippingAddress.CountryCode = ComposerConfiguration.CountryCode;
            }

            if (vm.Payment != null &&
                vm.Payment.BillingAddress != null &&
                string.IsNullOrWhiteSpace(vm.Payment.BillingAddress.CountryCode))
            {
                vm.Payment.BillingAddress.CountryCode = ComposerConfiguration.CountryCode;
            }
        }

        protected virtual void SetPhoneNumberRegexPattern(CartViewModel vm)
        {
            if (vm.ShippingAddress != null)
            {
                vm.ShippingAddress.PhoneRegex = CountryService.RetrieveCountryAsync(CountryParam).Result.PhoneRegex;
            }
        }

        protected virtual void SetPostalCodeRegexPattern(CartViewModel vm)
        {
            var regex = CountryService.RetrieveCountryAsync(CountryParam).Result.PostalCodeRegex;

            if (vm.ShippingAddress == null)
            {
                vm.ShippingAddress = new AddressViewModel();
            }

            vm.ShippingAddress.PostalCodeRegexPattern = regex;

            if (vm.Payment == null)
            {
                vm.Payment = new PaymentViewModel
                {
                    BillingAddress = new BillingAddressViewModel()
                };
            }

            vm.Payment.BillingAddress.PostalCodeRegexPattern = regex;
        }

        /// <summary>
        /// Gets the AdditionalFeeSummaryViewModel.
        /// </summary>
        /// <param name="lineItemDetailViewModels"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public virtual List<AdditionalFeeSummaryViewModel> GetAdditionalFeesSummary(
            IEnumerable<LineItemDetailViewModel> lineItemDetailViewModels,
            CultureInfo cultureInfo)
        {
            var additionalFeesList = new List<AdditionalFeeViewModel>();

            if (lineItemDetailViewModels == null) { return new List<AdditionalFeeSummaryViewModel>(); }

            foreach (var lineItemDetailViewModel in lineItemDetailViewModels
                .Where(lineItemDetailViewModel => lineItemDetailViewModel.AdditionalFees != null))
            {
                additionalFeesList.AddRange(lineItemDetailViewModel.AdditionalFees);
            }

            var taxableAdditionalFeesGroups = additionalFeesList.Where(a => a.Taxable).GroupBy(a => a.DisplayName);

            var additionalFeeSummaryList = taxableAdditionalFeesGroups
                .Select(additionalFeesGroup => new AdditionalFeeSummaryViewModel
                {
                    GroupName = additionalFeesGroup.Key,
                    TotalAmount = LocalizationProvider.FormatPrice(additionalFeesGroup.Sum(a => a.TotalAmount), cultureInfo),
                    Taxable = true
                }).ToList();

            var nonTaxableAdditionalFeesGroups = additionalFeesList.Where(a => !a.Taxable).GroupBy(a => a.DisplayName);

            additionalFeeSummaryList.AddRange(nonTaxableAdditionalFeesGroups
                .Select(additionalFeesGroup => new AdditionalFeeSummaryViewModel
                {
                    GroupName = additionalFeesGroup.Key,
                    TotalAmount = LocalizationProvider.FormatPrice(additionalFeesGroup.Sum(a => a.TotalAmount), cultureInfo),
                    Taxable = false
                }).ToList());

            return additionalFeeSummaryList;
        }

        protected virtual string GetShippingPrice(decimal cost, CultureInfo cultureInfo)
        {
            var price = cost == 0
                ? GetFreeShippingPriceLabel(cultureInfo)
                : LocalizationProvider.FormatPrice(cost, cultureInfo);

            return price;
        }

        public virtual IList<OrderShippingMethodViewModel> GetShippingsViewModel(
           Overture.ServiceModel.Orders.Cart cart, CultureInfo cultureInfo)
        {
            var shipments = cart.GetActiveShipments().Any() ?
                cart.GetActiveShipments().Where(x => x.FulfillmentMethod != null) :
                cart.Shipments.Where(x => x.FulfillmentMethod != null); //cancelled orders

            if (!shipments.Any() || !shipments.Where(x => x.FulfillmentMethod != null).Any())
            {
                return Enumerable.Empty<OrderShippingMethodViewModel>().ToList();
            }

            var formatTaxable = LocalizationProvider.GetLocalizedString(new GetLocalizedParam
            {
                Category = "ShoppingCart",
                Key = "L_ShippingBasedOn",
                CultureInfo = cultureInfo
            });
            var formatNonTaxable = LocalizationProvider.GetLocalizedString(new GetLocalizedParam
            {
                Category = "ShoppingCart",
                Key = "L_ShippingBasedOnNonTaxable",
                CultureInfo = cultureInfo
            });

            var shippings = shipments.GroupBy(x => x.IsShippingTaxable()).Select(shippingGroup => new OrderShippingMethodViewModel
            {
                Taxable = shippingGroup.Key,
                Cost = GetShippingPrice(Convert.ToDecimal(shippingGroup.Sum(x => x.FulfillmentMethod.Cost)), cultureInfo),
                DisplayName = shippingGroup.Key ?
                    string.Format(formatTaxable, shippingGroup.FirstOrDefault().Address?.PostalCode) :
                    string.Format(formatNonTaxable, shippingGroup.FirstOrDefault().Address?.PostalCode)
            });

            return shippings.ToList();
        }

        /// <summary>
        /// Gets an OrderSummaryViewModel from a Cart.
        /// </summary>
        /// <param name="cart"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public virtual OrderSummaryViewModel GetOrderSummaryViewModel(
            Overture.ServiceModel.Orders.Cart cart,
            CultureInfo cultureInfo)
        {
            var orderSummary = ViewModelMapper.MapTo<OrderSummaryViewModel>(cart, cultureInfo);

            orderSummary.Shippings = GetShippingsViewModel(cart, cultureInfo);
            orderSummary.Shipping = GetShippingFee(cart, cultureInfo);
            orderSummary.IsShippingTaxable = cart.GetActiveShipments().FirstOrDefault().IsShippingTaxable(); //used in the cart/checkout
            orderSummary.HasReward = cart.DiscountTotal.HasValue && cart.DiscountTotal.Value > 0;
            orderSummary.CheckoutRedirectAction = GetCheckoutRedirectAction(cart);
            orderSummary.Rewards = RewardViewModelFactory.CreateViewModel(cart.GetActiveShipments().SelectMany(x => x.Rewards), cultureInfo, RewardLevel.FulfillmentMethod, RewardLevel.Shipment).ToList();
            var allLineItems = cart.GetActiveShipments().SelectMany(x => x.LineItems).ToList();
            decimal sumAllLineItemsSavings =
                Math.Abs(allLineItems.Sum(
                    l => decimal.Multiply(decimal.Subtract(l.CurrentPrice.GetValueOrDefault(0), l.DefaultPrice.GetValueOrDefault(0)), Convert.ToDecimal(l.Quantity))));

            decimal savingsTotal = decimal.Add(cart.DiscountTotal.GetValueOrDefault(0), sumAllLineItemsSavings);
            orderSummary.SavingsTotal = savingsTotal.Equals(0) ? string.Empty : LocalizationProvider.FormatPrice(savingsTotal, cultureInfo);

            return orderSummary;
        }

        protected virtual CheckoutRedirectActionViewModel GetCheckoutRedirectAction(Overture.ServiceModel.Orders.Cart cart)
        {
            var checkoutRedirectAction = new CheckoutRedirectActionViewModel();

            object lastCheckoutStep;

            if (cart.PropertyBag != null && cart.PropertyBag.TryGetValue(CartConfiguration.CartPropertyBagLastCheckoutStep, out lastCheckoutStep))
            {
                checkoutRedirectAction.LastCheckoutStep = (int)lastCheckoutStep;
            }

            //If there is no lineitem in the cart the checkout step is 0 (edit cart page).
            if (!cart.GetActiveShipments().Any() || !cart.GetActiveShipments().SelectMany(x => x.LineItems).Any())
            {
                checkoutRedirectAction.LastCheckoutStep = 0;
                return checkoutRedirectAction;
            }

            //If there is lineitem in the cart lastCheckoutStep can't be 0 but 1 (step 1).
            if (checkoutRedirectAction.LastCheckoutStep == 0)
            {
                checkoutRedirectAction.LastCheckoutStep = 1;
            }

            return checkoutRedirectAction;
        }

        protected virtual void MapShipmentsAndPayments(
            Overture.ServiceModel.Orders.Cart cart,
            CultureInfo cultureInfo,
            ProductImageInfo imageInfo,
            string baseUrl,
            Dictionary<string, string> paymentMethodDisplayNames,
            CartViewModel cartVm,
            List<Payment> payments)
        {
            if (cart.Shipments == null)
            {
                return;
            }

            var shipment = cart.GetActiveShipments().FirstOrDefault();

            if (shipment == null)
            {
                return;
            }

            MapOneShipment(shipment, cultureInfo, imageInfo, baseUrl, cartVm, cart);
            cartVm.Payment = GetPaymentViewModel(payments, shipment.Address, paymentMethodDisplayNames, cultureInfo);
        }

        /// <summary>
        /// Maps a single shipment.
        /// </summary>
        /// <param name="shipment">Shipment to map.</param>
        /// <param name="cultureInfo">Culture Info.</param>
        /// <param name="imageInfo">Information about images</param>
        /// <param name="baseUrl">The request base url</param>
        /// <param name="cartVm">VM in which to map the shipment to.</param>
        /// <param name="cart">Cart being mapped.</param>
        protected virtual void MapOneShipment(
            Shipment shipment,
            CultureInfo cultureInfo,
            ProductImageInfo imageInfo,
            string baseUrl,
            CartViewModel cartVm,
            Overture.ServiceModel.Orders.Cart cart)
        {
            cartVm.CurrentShipmentId = shipment.Id;
            cartVm.Rewards = RewardViewModelFactory.CreateViewModel(shipment.Rewards, cultureInfo, RewardLevel.FulfillmentMethod, RewardLevel.Shipment).ToList();
            cartVm.OrderSummary.Taxes = TaxViewModelFactory.CreateTaxViewModels(shipment.Taxes, cultureInfo).ToList();
            cartVm.ShippingAddress = GetAddressViewModel(shipment.Address, cultureInfo);

            cartVm.LineItemDetailViewModels = LineItemViewModelFactory.CreateViewModel(new CreateListOfLineItemDetailViewModelParam
            {
                Cart = cart,
                LineItems = shipment.LineItems,
                CultureInfo = cultureInfo,
                ImageInfo = imageInfo,
                BaseUrl = baseUrl
            }).ToList();

            MapLineItemQuantifiers(cartVm);

            cartVm.OrderSummary.IsShippingEstimatedOrSelected = IsShippingEstimatedOrSelected(shipment);
            cartVm.ShippingMethod = GetShippingMethodViewModel(shipment.FulfillmentMethod, cultureInfo);

#pragma warning disable 618
            MapShipmentAdditionalFees(shipment, cartVm.OrderSummary, cultureInfo);
#pragma warning restore 618
        }

        protected virtual void MapCustomer(CustomerSummary customer, CultureInfo cultureInfo, CartViewModel cartVm)
        {
            if (customer == null)
            {
                return;
            }

            var customerViewModel = ViewModelMapper.MapTo<CustomerSummaryViewModel>(customer, cultureInfo);
            cartVm.Customer = customerViewModel;
        }

        /// <summary>
        /// Map the shipment additionnal fees to the orderSummaryViewModel.
        /// </summary>
        /// <param name="shipment"></param>
        /// <param name="viewModel"></param>
        /// <param name="cultureInfo"></param>
        [Obsolete("Use MapShipmentsAdditionalFees instead")]
        public virtual void MapShipmentAdditionalFees(Shipment shipment, OrderSummaryViewModel viewModel, CultureInfo cultureInfo)
        {
            var shipmentAdditionalFees = GetShipmentAdditionalFees(shipment.AdditionalFees, cultureInfo).ToList();
            viewModel.ShipmentAdditionalFeeAmount = shipment.AdditionalFeeAmount.ToString();
            viewModel.ShipmentAdditionalFeeSummaryList = GetShipmentAdditionalFeeSummary(shipmentAdditionalFees, cultureInfo);
        }

        /// <summary>
        /// Map the shipment additionnal fees to the orderSummaryViewModel considering all shipments
        /// </summary>
        /// <param name="shipments"></param>
        /// <param name="viewModel"></param>
        /// <param name="cultureInfo"></param>
        public virtual void MapShipmentsAdditionalFees(IEnumerable<Shipment> shipments, OrderSummaryViewModel viewModel, CultureInfo cultureInfo)
        {
            var enumerable = shipments as IList<Shipment> ?? shipments.ToList();
            var allShipmentAdditionalFees = enumerable.SelectMany(x => x.AdditionalFees).ToList();
            var shipmentAdditionalFees = GetShipmentAdditionalFees(allShipmentAdditionalFees, cultureInfo).ToList();
            viewModel.ShipmentAdditionalFeeAmount = enumerable.Sum(s => s.AdditionalFeeAmount).ToString();
            viewModel.ShipmentAdditionalFeeSummaryList = GetShipmentAdditionalFeeSummary(shipmentAdditionalFees, cultureInfo);
        }

        protected virtual IEnumerable<ShipmentAdditionalFeeViewModel> GetShipmentAdditionalFees(IEnumerable<ShipmentAdditionalFee> additionalFees, CultureInfo cultureInfo)
        {
            return additionalFees.Select(shipmentAdditionalFee => ViewModelMapper.MapTo<ShipmentAdditionalFeeViewModel>(shipmentAdditionalFee, cultureInfo));
        }

        public virtual List<AdditionalFeeSummaryViewModel> GetShipmentAdditionalFeeSummary(IEnumerable<ShipmentAdditionalFeeViewModel> shipmentAdditionalFeeViewModels, CultureInfo cultureInfo)
        {
            var shipmentAdditionalFeeSummaryList = new List<AdditionalFeeSummaryViewModel>();

            if (shipmentAdditionalFeeViewModels == null) { return shipmentAdditionalFeeSummaryList; }

            var additionalFeeViewModels = shipmentAdditionalFeeViewModels as IList<ShipmentAdditionalFeeViewModel> ?? shipmentAdditionalFeeViewModels.ToList();

            var taxableAdditionalFeesGroups = additionalFeeViewModels.Where(a => a.Taxable).GroupBy(a => a.DisplayName);

            shipmentAdditionalFeeSummaryList
                .AddRange(taxableAdditionalFeesGroups.Select(additionalFeesGroup => new AdditionalFeeSummaryViewModel
                {
                    GroupName = additionalFeesGroup.Key,
                    TotalAmount = LocalizationProvider.FormatPrice(additionalFeesGroup.Sum(a => a.Amount), cultureInfo),
                    Taxable = true
                }));

            var nonTaxableAdditionalFeesGroups = additionalFeeViewModels.Where(a => !a.Taxable).GroupBy(a => a.DisplayName);

            shipmentAdditionalFeeSummaryList
                .AddRange(nonTaxableAdditionalFeesGroups.Select(additionalFeesGroup => new AdditionalFeeSummaryViewModel
                {
                    GroupName = additionalFeesGroup.Key,
                    TotalAmount = LocalizationProvider.FormatPrice(additionalFeesGroup.Sum(a => a.Amount), cultureInfo),
                    Taxable = false
                }));

            return shipmentAdditionalFeeSummaryList;
        }

        protected virtual bool IsShippingEstimatedOrSelected(Shipment shipment)
        {
            if (shipment == null)
            {
                return false;
            }

            if (shipment.Address == null)
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(shipment.Address.PostalCode);
        }

        /// <summary>
        /// Gets a ShippingMethodViewModel from an Overture FulfillmentMethod object.
        /// </summary>
        /// <param name="fulfillmentMethod"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public virtual ShippingMethodViewModel GetShippingMethodViewModel(FulfillmentMethod fulfillmentMethod, CultureInfo cultureInfo)
        {
            if (fulfillmentMethod == null)
            {
                return null;
            }

            var shippingMethodViewModel = ViewModelMapper.MapTo<ShippingMethodViewModel>(fulfillmentMethod, cultureInfo);

            if (!fulfillmentMethod.ExpectedDeliveryDate.HasValue) { return shippingMethodViewModel; }

            var totalDays = (int)Math.Ceiling((fulfillmentMethod.ExpectedDeliveryDate.Value - DateTime.UtcNow).TotalDays);
            shippingMethodViewModel.ExpectedDaysBeforeDelivery = totalDays.ToString();

            shippingMethodViewModel.IsShipToStoreType = fulfillmentMethod.FulfillmentMethodType == FulfillmentMethodType.ShipToStore;
            shippingMethodViewModel.FulfillmentMethodTypeString = fulfillmentMethod.FulfillmentMethodType.ToString();

            return shippingMethodViewModel;
        }

        /// <summary>
        /// Gets a PaymentMethodViewModel from an Overture PaymentMethod object.
        /// </summary>
        /// <param name="paymentMethod"></param>
        /// <param name="paymentMethodDisplayNames"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public virtual IPaymentMethodViewModel GetPaymentMethodViewModel(
            PaymentMethod paymentMethod,
            Dictionary<string, string> paymentMethodDisplayNames,
            CultureInfo cultureInfo)
        {
            if (paymentMethod == null)
            {
                return null;
            }

            if (paymentMethodDisplayNames == null)
            {
                return null;
            }

            var paymentMethodDisplayName = paymentMethodDisplayNames.FirstOrDefault(x => x.Key == paymentMethod.Type.ToString()).Value;

            if (paymentMethodDisplayName == null)
            {
                return null;
            }

            IPaymentMethodViewModel paymentMethodViewModel;
            switch (paymentMethod.Type)
            {
                case PaymentMethodType.SavedCreditCard:
                    paymentMethodViewModel = MapSavedCreditCard(paymentMethod, cultureInfo);
                    break;

                default:
                    var vm = ViewModelMapper.MapTo<PaymentMethodViewModel>(paymentMethod, cultureInfo);
                    vm.DisplayName = paymentMethodDisplayName;
                    paymentMethodViewModel = vm;
                    break;
            }

            paymentMethodViewModel.PaymentType = paymentMethod.Type.ToString();

            return paymentMethodViewModel;
        }

        public virtual SavedCreditCardPaymentMethodViewModel MapSavedCreditCard(PaymentMethod payment, CultureInfo cultureInfo)
        {            
            var savedCreditCard = ViewModelMapper.MapTo<SavedCreditCardPaymentMethodViewModel>(payment, cultureInfo);

            if (!string.IsNullOrWhiteSpace(savedCreditCard.ExpiryDate))
            {
                var expirationDate = ParseCreditCartExpiryDate(savedCreditCard.ExpiryDate);
                expirationDate = expirationDate.AddDays(DateTime.DaysInMonth(expirationDate.Year, expirationDate.Month) - 1);
                savedCreditCard.IsExpired = expirationDate < DateTime.UtcNow;
            }

            return savedCreditCard;
        }

        protected virtual DateTime ParseCreditCartExpiryDate(string expiryDate)
        {
            var formats = new[]
            {
                "MMyy",
                "MM/yy",
                "MM-yy",
            };

            return DateTime.ParseExact(expiryDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        /// <summary>
        /// Map the address of the client
        /// </summary>
        /// <param name="address"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public virtual AddressViewModel GetAddressViewModel(Address address, CultureInfo cultureInfo)
        {
            if (address == null)
            {
                return new AddressViewModel();
            }

            var addressViewModel = ViewModelMapper.MapTo<AddressViewModel>(address, cultureInfo);

            var regionName = CountryService.RetrieveRegionDisplayNameAsync(new RetrieveRegionDisplayNameParam
            {
                CultureInfo = cultureInfo,
                IsoCode = ComposerContext.CountryCode,
                RegionCode = address.RegionCode
            }).Result;

            addressViewModel.RegionName = regionName;
            addressViewModel.PhoneNumber = LocalizationProvider.FormatPhoneNumber(address.PhoneNumber, cultureInfo);

            return addressViewModel;
        }

        protected virtual void MapLineItemQuantifiers(CartViewModel cartViewModel)
        {
            cartViewModel.IsCartEmpty = true;

            if (cartViewModel.LineItemDetailViewModels == null) { return; }

            foreach (var lineItemDetailViewModel in cartViewModel.LineItemDetailViewModels)
            {
                cartViewModel.IsCartEmpty = false;
                cartViewModel.LineItemCount += 1;
                cartViewModel.TotalQuantity += (int)lineItemDetailViewModel.Quantity;
                cartViewModel.InvalidLineItemCount += lineItemDetailViewModel.IsValid.GetValueOrDefault(true) ? 0 : 1;
            }
        }

        protected virtual string GetShippingFee(Overture.ServiceModel.Orders.Cart cart, CultureInfo cultureInfo)
        {
            if (!cart.GetActiveShipments().Any())
            {
                return GetFreeShippingPriceLabel(cultureInfo);
            }

            if (cart.FulfillmentCost.HasValue)
            {
                return GetShippingPrice(cart.FulfillmentCost.Value, cultureInfo);
            }

            var shippingMethodParam = new GetShippingMethodsParam
            {
                CartName = cart.Name,
                CultureInfo = cultureInfo,
                CustomerId = cart.CustomerId,
                Scope = cart.ScopeId
            };

            //TODO: Remove the repository and pass the list of fulfillmentMethods in params
            var fulfillmentMethods = FulfillmentMethodRepository.GetCalculatedFulfillmentMethods(shippingMethodParam).Result;

            if (fulfillmentMethods == null || !fulfillmentMethods.Any())
            {
                return GetFreeShippingPriceLabel(cultureInfo);
            }

            var cheapestShippingCost = fulfillmentMethods.Min(s => (decimal)s.Cost);

            var price = GetShippingPrice(cheapestShippingCost, cultureInfo);

            return price;
        }

        protected virtual string GetFreeShippingPriceLabel(CultureInfo cultureInfo)
        {
            return LocalizationProvider.GetLocalizedString(new GetLocalizedParam
            {
                Category = "ShoppingCart",
                Key = "L_Free",
                CultureInfo = cultureInfo
            });
        }

        public virtual CouponsViewModel GetCouponsViewModel(
            Overture.ServiceModel.Orders.Cart cart,
            CultureInfo cultureInfo,
            bool includeMessages)
        {
            if (cart?.Coupons?.FirstOrDefault() == null)
            {
                return new CouponsViewModel();
            }

            List<Coupon> coupons = cart.Coupons;

            var couponsVm = new CouponsViewModel
            {
                ApplicableCoupons = new List<CouponViewModel>(),
                Messages = new List<CartMessageViewModel>()
            };

            var allRewards = GetAllRewards(cart.GetActiveShipments().ToList());

            foreach (var coupon in coupons)
            {
                if (IsCouponValid(coupon))
                {
                    var reward = allRewards.FirstOrDefault(r => r.PromotionId == coupon.PromotionId);
                    var couponVm = MapCoupon(coupon, reward, cultureInfo);
                    couponsVm.ApplicableCoupons.Add(couponVm);
                }
                else if (includeMessages)
                {
                    var message = GetInvalidCouponMessage(coupon, cultureInfo);
                    couponsVm.Messages.Add(message);
                }
            }
            return couponsVm;
        }
        /// <summary>
        /// Get all rewards applied on line items and shipment into a single list
        /// </summary>
        /// <param name="orderShipment"></param>
        /// <returns></returns>
        protected List<Reward> GetAllRewards(List<Shipment> orderShipment)
        {
            var allRewards = new List<Reward>();
            if (orderShipment.Any())
            {
                var shipmentRewards = Enumerable.Empty<Reward>();
                var lineItemRewards = Enumerable.Empty<Reward>();

                shipmentRewards = orderShipment.SelectMany(s => s.Rewards);

                if (orderShipment?.SelectMany(s => s.LineItems?.SelectMany(l => l.Rewards)) != null)
                {
                    lineItemRewards = orderShipment
                        .SelectMany(t => t.LineItems
                            .SelectMany(l => l.Rewards));
                }

                allRewards = shipmentRewards
                    .Concat(lineItemRewards)
                    .ToList();
            }
            return allRewards;
        }

        protected virtual bool IsCouponValid(Coupon coupon)
        {
            return coupon.CouponState == CouponState.Ok;
        }

        protected virtual CouponViewModel MapCoupon(Coupon coupon, Reward reward, CultureInfo cultureInfo)
        {
            var vm = ViewModelMapper.MapTo<CouponViewModel>(coupon, cultureInfo);
            if (reward != null)
            {
                vm.Amount = reward.Amount;
                vm.PromotionName = reward.PromotionName;
            }
            return vm;
        }

        protected virtual CartMessageViewModel GetInvalidCouponMessage(Coupon coupon, CultureInfo cultureInfo)
        {
            var localizedMessageTemplate = LocalizationProvider.GetLocalizedString(new GetLocalizedParam
            {
                Category = "ShoppingCart",
                Key = GetLocalizedKey(coupon.CouponState),
                CultureInfo = cultureInfo
            });

            var localizedMessage = string.Format(localizedMessageTemplate, coupon.CouponCode);

            var message = new CartMessageViewModel
            {
                Message = localizedMessage,
                Level = CartMessageLevels.Error
            };

            return message;
        }

        private static string GetLocalizedKey(CouponState couponState)
        {
            return couponState == CouponState.ValidCouponCannotApply
                ? "F_PromoCodeValidCouponCannotApply"
                : "F_PromoCodeInvalid";
        }

        /// <summary>
        /// Returns the fist payment non voided.
        /// </summary>
        protected virtual PaymentViewModel GetPaymentViewModel(
            List<Payment> payments,
            Address shippingAddress,
            Dictionary<string, string> paymentMethodDisplayNames,
            CultureInfo cultureInfo)
        {
            if (payments == null)
            {
                return BuildEmptyPaymentViewModel();
            }

            var validPayments = payments.Where(x => !x.IsVoided()).ToList();

            if (!validPayments.Any())
            {
                return BuildEmptyPaymentViewModel();
            }

            var payment = validPayments.First();

            IPaymentMethodViewModel paymentMethodViewModel = null;
            if (payment.PaymentMethod != null)
            {
                paymentMethodViewModel = GetPaymentMethodViewModel(payment.PaymentMethod, paymentMethodDisplayNames, cultureInfo);
            }

            var billingAddressViewModel = GetBillingAddressViewModel(payment.BillingAddress, shippingAddress, cultureInfo);

            return new PaymentViewModel
            {
                Id = payment.Id,
                IsLocked = payment.PaymentStatus != PaymentStatus.New && payment.PaymentStatus != PaymentStatus.PendingVerification,
                PaymentMethod = paymentMethodViewModel,
                PaymentStatus = payment.PaymentStatus,
                BillingAddress = billingAddressViewModel
            };
        }

        protected static PaymentViewModel BuildEmptyPaymentViewModel()
        {
            return new PaymentViewModel
            {
                IsLocked = false,
                PaymentStatus = PaymentStatus.New,
                BillingAddress = new BillingAddressViewModel { UseShippingAddress = true }
            };
        }

        protected virtual BillingAddressViewModel GetBillingAddressViewModel(Address billingAddress, Address shippingAddress, CultureInfo cultureInfo)
        {
            if (billingAddress == null)
            {
                return new BillingAddressViewModel { UseShippingAddress = true };
            }

            var billingAddressViewModel = MapBillingAddressViewModel(billingAddress, cultureInfo);
            billingAddressViewModel.UseShippingAddress = shippingAddress != null && AreAddressEqual(shippingAddress, billingAddress);

            return billingAddressViewModel;
        }

        protected virtual BillingAddressViewModel MapBillingAddressViewModel(Address address, CultureInfo cultureInfo)
        {
            if (address == null)
            {
                return null;
            }

            var addressViewModel = ViewModelMapper.MapTo<BillingAddressViewModel>(address, cultureInfo);

            var regionName = CountryService.RetrieveRegionDisplayNameAsync(new RetrieveRegionDisplayNameParam
            {
                CultureInfo = cultureInfo,
                IsoCode = ComposerContext.CountryCode,
                RegionCode = address.RegionCode
            }).Result;

            addressViewModel.RegionName = regionName;

            return addressViewModel;
        }

        protected virtual bool AreAddressEqual(Address first, Address second)
        {
            return first.FirstName == second.FirstName
                   && first.LastName == second.LastName
                   && first.Line1 == second.Line1
                   && first.Line2 == second.Line2
                   && first.City == second.City
                   && first.RegionCode == second.RegionCode
                   && first.CountryCode == second.CountryCode
                   && first.PostalCode == second.PostalCode
                   && first.PhoneNumber == second.PhoneNumber;
        }
    }
}