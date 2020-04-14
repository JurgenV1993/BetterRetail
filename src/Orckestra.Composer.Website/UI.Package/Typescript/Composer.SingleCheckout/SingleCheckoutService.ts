///<reference path='../../Typings/tsd.d.ts' />
///<reference path='../../Typings/vue/index.d.ts' />
///<reference path='../Composer.MyAccount/Common/MembershipService.ts' />
///<reference path='../ErrorHandling/ErrorHandler.ts' />
///<reference path='../Repositories/CartRepository.ts' />
///<reference path='../Composer.Cart/CheckoutShippingMethod/ShippingMethodService.ts' />
///<reference path='../Composer.Cart/CartSummary/CartService.ts' />
///<reference path='./IBaseSingleCheckoutController.ts' />
///<reference path='../Composer.Cart/CheckoutCommon/RegionService.ts' />
///<reference path='../Composer.Cart/CheckoutCommon/ICheckoutService.ts' />
///<reference path='../Composer.Cart/CheckoutCommon/ICheckoutContext.ts' />
///<reference path='../Composer.Cart/CheckoutCommon/IRegisterOptions.ts' />
///<reference path='../Composer.Cart/CheckoutPayment/Services/PaymentService.ts' />
///<reference path='../Composer.Cart/CheckoutPayment/Repositories/PaymentRepository.ts' />
///<reference path='../Composer.Cart/CheckoutPayment/Providers/CheckoutPaymentProviderFactory.ts' />
///<reference path='./ISingleCheckoutService.ts' />
///<reference path='./ISingleCheckoutContext.ts' />
///<reference path='../Composer.MyAccount/Common/CustomerService.ts' />
///<reference path='./VueComponents/CheckoutStepVueComponent.ts' />
///<reference path='./VueComponents/CheckoutPageVueComponent.ts' />


module Orckestra.Composer {
    'use strict';

    export enum CheckoutStepNumbers {
        Information = 0,
        Shipping = 1,
        ReviewCart = 2,
        Billing = 3,
        Payment = 4
    }

    export enum FulfillmentMethodTypes {
        Shipping = 'Shipping',
        PickUp = 'PickUp'
    }

    export class SingleCheckoutService implements ISingleCheckoutService {

        private static instance: ISingleCheckoutService;

        public VueCheckout: Vue;
        public VueCheckoutMixins: any = [];

        private orderConfirmationCacheKey = 'orderConfirmationCacheKey';
        private orderCacheKey = 'orderCacheKey';

        private readonly window: Window;
        private readonly eventHub: IEventHub;
        private registeredControllers: any = {};
        private allControllersReady: Q.Deferred<boolean>;
        private cacheProvider: ICacheProvider;

        protected cartService: ICartService;
        protected membershipService: IMembershipService;
        protected customerService: ICustomerService = new CustomerService(new CustomerRepository());
        protected regionService: IRegionService;
        protected shippingMethodService: ShippingMethodService;
        protected paymentService: IPaymentService;
        protected paymentProviderFactory: CheckoutPaymentProviderFactory;

        public static getInstance(): ISingleCheckoutService {

            if (!SingleCheckoutService.instance) {
                SingleCheckoutService.instance = new SingleCheckoutService();
            }

            return SingleCheckoutService.instance;
        }

        public constructor() {

            if (SingleCheckoutService.instance) {
                throw new Error('Instantiation failed: Use SingleCheckoutService.instance() instead of new.');
            }

            this.eventHub = EventHub.instance();
            this.window = window;
            this.allControllersReady = Q.defer<boolean>();
            this.cacheProvider = CacheProvider.instance();

            this.cartService = new CartService(new CartRepository(), this.eventHub);
            this.membershipService = new MembershipService(new MembershipRepository());
            this.regionService = new RegionService();
            this.shippingMethodService = new ShippingMethodService();
            this.paymentService = new PaymentService(this.eventHub, new PaymentRepository());
            this.paymentProviderFactory = new CheckoutPaymentProviderFactory(this.window, this.eventHub);
            this.registerAllControllersInitialized();

            SingleCheckoutService.instance = this;
        }

        protected registerAllControllersInitialized(): void {

            this.eventHub.subscribe('allControllersInitialized', () => {
                this.initialize();
            });
        }

        private initialize() {

            let authenticatedPromise = this.membershipService.isAuthenticated();
            let getCartPromise = this.getCart();
            let regionsPromise: Q.Promise<any> = this.regionService.getRegions();
            let shippingMethodTypesPromise: Q.Promise<any> = this.shippingMethodService.getShippingMethodTypes();

            Q.all([authenticatedPromise, getCartPromise, regionsPromise, shippingMethodTypesPromise])
                .spread((authVm, cartVm, regionsVm, shippingMethodTypesVm) => {

                    if (!cartVm.Customer) {
                        cartVm.Customer = {};
                    }

                    let results: ISingleCheckoutContext = {
                        IsAuthenticated: authVm.IsAuthenticated,
                        Cart: cartVm,
                        Regions: regionsVm,
                        ShippingMethodTypes: shippingMethodTypesVm.ShippingMethodTypes,
                        Payment: null
                    };

                    this.handleCheckoutSecurity(cartVm);

                    this.initializeVueComponent(results);
                })
                .then(() => {
                    this.allControllersReady.resolve(true);
                })
                .fail((reason: any) => {
                    console.error('Error while initializing SingleCheckoutService.', reason);
                    ErrorHandler.instance().outputErrorFromCode('CheckoutRenderFailed');
                });
        }

        private handleCheckoutSecurity(cart: any) {
            if (cart.IsCartEmpty && !Utils.IsC1ConsolePreview()) {
                this.window.location.href = cart.OrderSummary.CheckoutRedirectAction.RedirectUrl;
            }
        }

        private initializeVueComponent(checkoutContext: ISingleCheckoutContext) {
            let startStep = this.calculateStartStep(checkoutContext.Cart, checkoutContext.IsAuthenticated);
            this.VueCheckout = new Vue({
                el: '#vueSingleCheckout',
                components: {
                    [CheckoutPageVueComponent.componentMame] : CheckoutPageVueComponent.getComponent(),
                    [CheckoutStepVueComponent.componentMame] : CheckoutStepVueComponent.getComponent(),
                },
                data: {
                    Cart: checkoutContext.Cart,
                    Regions: checkoutContext.Regions,
                    ShippingMethodTypes: checkoutContext.ShippingMethodTypes,
                    Payment: null,
                    Steps: {
                        StartStep: startStep,
                        EnteredOnce: {
                            Information: true,
                            Shipping: false,
                            ReviewCart: false,
                            Billing: false,
                            Payment: false
                        }
                    },
                    Mode: {
                        AddingNewAddress: false,
                        AddingLine2Address: false,
                        CompleteCheckoutLoading: false,
                        Loading: false,
                        Authenticated: checkoutContext.IsAuthenticated
                    },
                    Errors: {
                        PostalCodeError: false,
                        InvalidPhoneFormatError: false,
                        AddressNameAlreadyInUseError: false,
                        StoreLocatorLocationError: false,
                    }
                },
                mixins: this.VueCheckoutMixins,
                mounted() {

                },
                computed: {
                    Customer() {
                        return this.Cart.Customer;
                    },
                    ShippingAddress() {
                        return this.Cart.ShippingAddress;
                    },
                    Rewards() {
                        return this.Cart.Rewards;
                    },
                    OrderSummary() {
                        return this.Cart.OrderSummary;
                    },
                    CartEmpty() {
                        return this.Cart.LineItemDetailViewModels.length == 0;
                    },
                    IsLoading() {
                        return this.Mode.Loading;
                    },
                    IsAuthenticated() {
                        return this.Mode.Authenticated;
                    }
                },
                methods: {

                    initializeParsey(formId: any): boolean {
                        let parsleyInit = $(formId).parsley();
                        if (parsleyInit) {
                            parsleyInit.validate();
                            return parsleyInit.isValid();
                        }
                        return true;
                    }
                }
            });
        }

        public calculateStartStep(cart: any, isAuthenticated: boolean): number {
            if (!(cart.Customer.FirstName &&
                cart.Customer.LastName &&
                cart.Customer.Email)) {
                return CheckoutStepNumbers.Information
            } else {
                if (!(this.isShippingFulfilled(cart, isAuthenticated))) {
                    return CheckoutStepNumbers.Shipping
                } else {
                    return CheckoutStepNumbers.Billing
                }
            }
        }

        private isShippingFulfilled(cart: any, isAuthenticated: boolean): boolean {
            if (!(cart.ShippingMethod)) return false;

            let address = cart.ShippingAddress.Line1 &&
                cart.ShippingAddress.City &&
                cart.ShippingAddress.RegionCode &&
                cart.ShippingAddress.PostalCode;

            let isShipToHome = cart.ShippingMethod.FulfillmentMethodTypeString === FulfillmentMethodTypes.Shipping;
            let isPickUp = cart.ShippingMethod.FulfillmentMethodTypeString === FulfillmentMethodTypes.PickUp;

            if (isAuthenticated && isShipToHome) {
                return (address && !this.isAddressBookIdEmpty(cart.ShippingAddress.AddressBookId))
            }

            if (!isAuthenticated && isShipToHome) {
                return !!(address)
            }

            if (isPickUp) {
                return (address && cart.ShippingAddress.AddressName)
            }

            return false;
        }

        private isAddressBookIdEmpty(bookId) {
            return bookId == '00000000-0000-0000-0000-000000000000' || !bookId;
        }

        public registerController(controller: IBaseSingleCheckoutController) {

            if (this.allControllersReady.promise.isPending()) {
                this.allControllersReady.resolve(false);
            }

            this.allControllersReady.promise
                .then(allControllersReady => {

                    if (allControllersReady) {
                        throw new Error('Too late to register all controllers are ready.');
                    } else {
                        let controllerName = controller.viewModelName;
                        this.registeredControllers[controllerName] = controller;
                    }
                });
        }

        public unregisterController(controllerName: string) {

            delete this.registeredControllers[controllerName];
        }

        public updatePostalCode(postalCode: string): Q.Promise<void> {

            return this.cartService.updateShippingMethodPostalCode(postalCode);
        }

        public invalidateCache(): Q.Promise<void> {

            return this.cartService.invalidateCache();
        }

        public getCart(): Q.Promise<any> {

            return this.invalidateCache()
                .then(() => this.cartService.getCart())
                .fail(reason => {
                    this.handleError(reason);
                });
        }

        public removeCartItem(id: any, productId: any): Q.Promise<any> {

            return this.invalidateCache().
                then(() => this.cartService.deleteLineItem(id, productId))
                .fail(reason => {
                    this.handleError(reason);
                });
        }

        public updateCartItem(id: string, quantity: number, productId: string,
            recurringOrderFrequencyName?: string,
            recurringOrderProgramName?: string): Q.Promise<any> {

            return this.invalidateCache().
                then(() => this.cartService.updateLineItem(id, quantity, productId, recurringOrderFrequencyName, recurringOrderProgramName))
                .fail(reason => {
                    this.handleError(reason);
                });
        }

        public updateCart(controllerNames: Array<string> = null): Q.Promise<IUpdateCartResult> {
            let emptyVm = {
                UpdatedCart: {}
            };
            let vue: any = this.VueCheckout;
            vue.Mode.Loading = true;

            return this.buildCartUpdateViewModel(emptyVm, controllerNames)
                .then(vm => this.cartService.updateCart(vm))
                .then(result => {
                    let { Cart } = result;
                    vue.customerBeforeEdit = { ...Cart.Customer };
                    vue.adressBeforeEdit = { ...Cart.ShippingAddress };
                    vue.billingAddressBeforeEdit = { ...Cart.Payment.BillingAddress };
                    vue.Errors = {};
                    vue.Cart = Cart;
                    return result;
                })
                .finally(() => {
                    vue.Mode.Loading = false;
                });
        }


        public collectViewModelNamesForUpdateCart(): Q.Promise<any> {
            let controllerInstance: IBaseSingleCheckoutController;
            let promises: Q.Promise<any>[] = [];

            for (let controllerName in this.registeredControllers) {

                if (this.registeredControllers.hasOwnProperty(controllerName)) {
                    controllerInstance = <IBaseSingleCheckoutController>this.registeredControllers[controllerName];
                    promises.push(controllerInstance.getViewModelNameForUpdatePromise());
                }
            }

            return Q.all(promises);
        }

        public updatePaymentMethod(param: any): Q.Promise<IActivePaymentViewModel> {
            let vue: any = this.VueCheckout;

            vue.Mode.Loading = true;
            return this.paymentService.updatePaymentMethod(param)
                .finally(() => {
                    vue.Mode.Loading = false;
                });
        }

        public completeCheckout(): Q.Promise<any> {
            console.log('completeCheckout(): Publishing the cart!');

            return this.cartService.completeCheckout()
                .then((result: ICompleteCheckoutResult) => {
                    if (_.isEmpty(result.OrderNumber)) {
                        throw {
                            message: 'We could not complete the order because the order number is empty',
                            data: result
                        };
                    }

                    this.eventHub.publish('checkoutCompleted', { data: result });
                    this.setOrderToCache(result);

                    this.setOrderConfirmationToCache(result);
                    if (result.NextStepUrl) {
                        window.location.href = result.NextStepUrl;
                    }
                })
                .fail(reason => {
                    console.error('An error occurred while completing the checkout.', reason);
                    ErrorHandler.instance().outputErrorFromCode('CompleteCheckoutFailed');
                });
        }

        private buildCartUpdateViewModel(vm: any, controllersName = null): Q.Promise<any> {

            //var validationPromise: Q.Promise<any>;
            //var viewModelUpdatePromise: Q.Promise<any>;

            // validationPromise = Q(vm).then(vm => {
            //     return this.getCartValidation(vm);
            // });

            //  viewModelUpdatePromise = validationPromise.then(vm => {
            return this.getCartUpdateViewModel(vm, controllersName);
            // });

            ///return viewModelUpdatePromise;
        }

        private getCartValidation(vm: any): Q.Promise<any> {

            let validationPromise = this.collectValidationPromises();

            return validationPromise.then((results: Array<boolean>) => {
                console.log('Aggregating all validation results');
                let hasFailedValidation = _.any(results, r => !r);

                if (hasFailedValidation) {
                    throw new Error('There were validation errors.');
                }

                return vm;
            });
        }

        private getCartUpdateViewModel(vm: any, controllersName = null): Q.Promise<any> {

            let updateModelPromise = this.collectUpdateModelPromises(controllersName);

            return updateModelPromise.then((updates: Array<any>) => {
                console.log('Aggregating all ViewModel updates.');

                _.each(updates, update => {

                    if (update) {
                        var keys = _.keys(update);
                        _.each(keys, key => {
                            vm.UpdatedCart[key] = update[key];
                        });
                    }
                });

                return vm;
            });
        }

        private collectValidationPromises(): Q.Promise<any> {

            let promises: Q.Promise<any>[] = [];
            let controllerInstance: IBaseSingleCheckoutController;

            for (let controllerName in this.registeredControllers) {

                if (this.registeredControllers.hasOwnProperty(controllerName)) {
                    controllerInstance = <IBaseSingleCheckoutController>this.registeredControllers[controllerName];
                    promises.push(controllerInstance.getValidationPromise());
                }
            }

            return Q.all(promises);
        }

        private collectUpdateModelPromises(controllerNames = null): Q.Promise<any> {

            let promises: Q.Promise<any>[] = [];
            let controllerInstance: IBaseSingleCheckoutController;

            for (let controllerName in this.registeredControllers) {

                if (controllerNames && !controllerNames.find(i => i === controllerName)) {
                    continue;
                }

                if (this.registeredControllers.hasOwnProperty(controllerName)) {
                    controllerInstance = <IBaseSingleCheckoutController>this.registeredControllers[controllerName];
                    promises.push(controllerInstance.getUpdateModelPromise());
                }
            }


            return Q.all(promises);
        }

        private handleError(reason: any): void {

            console.error('Unable to retrieve the cart for the checkout', reason);

            throw reason;
        }

        public setOrderConfirmationToCache(orderConfirmationViewModel: any): void {

            this.cacheProvider.defaultCache.set(this.orderConfirmationCacheKey, orderConfirmationViewModel).done();
        }

        public getOrderConfirmationFromCache(): Q.Promise<any> {

            return this.cacheProvider.defaultCache.get<any>(this.orderConfirmationCacheKey);
        }

        public clearOrderConfirmationFromCache(): void {

            this.cacheProvider.defaultCache.clear(this.orderConfirmationCacheKey).done();
        }

        public setOrderToCache(orderConfirmationViewModel: any): void {

            this.cacheProvider.defaultCache.set(this.orderCacheKey, orderConfirmationViewModel).done();
        }

        public getPaymentProviders(paymentProviders: Array<any>): Array<BaseCheckoutPaymentProvider> {
            if (_.isEmpty(paymentProviders)) {
                console.error('No payment provider was found');
            }

            return paymentProviders.map((vm: any) => this.paymentProviderFactory.getInstance(vm.ProviderType, vm.ProviderName));
        }

        public getPaymentCheckout(): Q.Promise<ICheckoutPaymentViewModel> {
            return this.paymentService.getCheckoutPayment();
        }

        public updateBillingPostalCode(postalCode: string): Q.Promise<void> {
            return this.cartService.updateBillingMethodPostalCode(postalCode);
        }

        public saveAddressToMyAccountAddressBook(address: any): Q.Promise<any> {
            return this.customerService.createAddress(address, null).then(address => {
                var vue: any = this.VueCheckout;
                vue.RegisteredAddresses.push(address);
                return address;
            }
            );
        }
    }
}
