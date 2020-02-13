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


module Orckestra.Composer {
    'use strict';

    export class SingleCheckoutService implements ISingleCheckoutService {

        private static instance: ISingleCheckoutService;

        public static checkoutStep: number;

        private orderConfirmationCacheKey = 'orderConfirmationCacheKey';
        private orderCacheKey = 'orderCacheKey';

        private window: Window;
        private eventHub: IEventHub;
        private registeredControllers: any = {};
        private allControllersReady: Q.Deferred<boolean>;
        private cacheProvider: ICacheProvider;

        protected cartService: ICartService;
        protected membershipService: IMembershipService;
        protected regionService: IRegionService;
        protected shippingMethodService: ShippingMethodService;

        protected vueSingleCheckout: Vue;

        public static getInstance(): ISingleCheckoutService {

            if (!SingleCheckoutService.instance) {
                SingleCheckoutService.instance = new SingleCheckoutService();
            };

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
            this.registerAllControllersInitialized();

            SingleCheckoutService.instance = this;
        }

        protected registerAllControllersInitialized(): void {

            this.eventHub.subscribe('allControllersInitialized', () => {
                this.initialize();
            });
        }

        private initialize() {

            var authenticatedPromise = this.membershipService.isAuthenticated();
            var getCartPromise = this.getCart();
            var regionsPromise: Q.Promise<any> = this.regionService.getRegions();
            var shippingMethodsPromise: Q.Promise<any> = this.shippingMethodService.getShippingMethods();

            Q.all([authenticatedPromise, getCartPromise, regionsPromise, shippingMethodsPromise])
                .spread((authVm, cartVm, regionsVm, shippingMethodsVm) => {

                    var results: ICheckoutContext = {
                        authenticationViewModel: authVm,
                        cartViewModel: cartVm,
                        regionsViewModel: regionsVm,
                        shippingMethodsViewModel: shippingMethodsVm
                    };

                    this.initializeVueComponent(results);

                    //return this.renderControllers(results);
                })
                .then(() => {
                    this.allControllersReady.resolve(true);
                })
                .fail((reason: any) => {
                    console.error('Error while initializing CheckoutService.', reason);
                    ErrorHandler.instance().outputErrorFromCode('CheckoutRenderFailed');
                });
        }


        public initializeVueComponent(checkoutContext: ICheckoutContext) {

            this.vueSingleCheckout = new Vue({
                el: '#vueSingleCheckout',
                data: checkoutContext,
                mounted() {
                },
                computed: {

                    Customer()  { 
                        return this.cartViewModel.Customer;
                     },
                       
                    ShippingMethods() {
                        return  this.shippingMethodsViewModel.ShippingMethods;
                    },

                    OrderSummary () {
                        return this.cartViewModel.OrderSummary;
                    },

                    OrderCanBePlaced() {
                        let email = this.cartViewModel.Customer.Email;
                        return email &&  email.length > 0 ? true: false;
                    }
                }
            });

        }

        public registerController(controller: IBaseCheckoutController) {

            if (this.allControllersReady.promise.isPending()) {
                this.allControllersReady.resolve(false);
            }

            this.allControllersReady.promise
                .then(allControllersReady => {

                    if (allControllersReady) {
                        throw new Error('Too late to register all controllers are ready.');
                    } else {
                        var controllerName = controller.viewModelName;
                        this.registeredControllers[controllerName] = controller;
                    }
            });
        }

        public unregisterController(controllerName: string) {

            delete this.registeredControllers[controllerName];
        }


        public updatePostalCode(postalCode: string): Q.Promise<void> {

            return this.cartService.updateBillingMethodPostalCode(postalCode);
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

        public updateCart(): Q.Promise<IUpdateCartResult> {

            this.allControllersReady.promise
            .then(allControllersReady => {
                if (!allControllersReady) {
                    throw new Error('All registered controllers are not ready.');
                }
            });

            var emptyVm = {
                UpdatedCart: {}
            };

            return this.buildCartUpdateViewModel(emptyVm)
                .then(vm => this.cartService.updateCart(vm));
        }

        public completeCheckout(): Q.Promise<ICompleteCheckoutResult> {

            var emptyVm = {
                UpdatedCart: {}
            };

            return this.buildCartUpdateViewModel(emptyVm)
                .then(vm => {

                    if (_.isEmpty(vm.UpdatedCart)) {
                        console.log('No modification required to the cart.');
                        return vm;
                    }

                    return this.cartService.updateCart(vm);
                })
                .then(result => {

                    if (result && result.HasErrors) {
                        throw new Error('Error while updating the cart. Complete Checkout will not complete.');
                    }

                    console.log('Publishing the cart!');

                    return this.cartService.completeCheckout(CheckoutService.checkoutStep);
                });
        }

        private buildCartUpdateViewModel(vm: any): Q.Promise<any> {

            var validationPromise: Q.Promise<any>;
            var viewModelUpdatePromise: Q.Promise<any>;

            validationPromise = Q(vm).then(vm => {
                return this.getCartValidation(vm);
            });

            viewModelUpdatePromise = validationPromise.then(vm => {
                return this.getCartUpdateViewModel(vm);
            });

            return viewModelUpdatePromise;
        }

        private getCartValidation(vm : any): Q.Promise<any> {

            var validationPromise = this.collectValidationPromises();

            var promise = validationPromise.then((results : Array<boolean>) => {
                console.log('Aggregrating all validation results');
                var hasFailedValidation = _.any(results, r => !r);

                if (hasFailedValidation) {
                    throw new Error('There were validation errors.');
                }

                return vm;
            });

            return promise;
        }

        private getCartUpdateViewModel(vm: any): Q.Promise<any> {

            var updateModelPromise = this.collectUpdateModelPromises();

            var promise = updateModelPromise.then((updates: Array<any>) => {
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

            return promise;
        }

        private collectValidationPromises(): Q.Promise<any> {

            var promises: Q.Promise<any>[] = [];
            var controllerInstance: IBaseSingleCheckoutController;
            var controllerOptions: IRegisterControlOptions;

            for (var controllerName in this.registeredControllers) {

                if (this.registeredControllers.hasOwnProperty(controllerName)) {
                    controllerInstance = <IBaseCheckoutController>this.registeredControllers[controllerName];
                    promises.push(controllerInstance.getValidationPromise());
                }
            }

            return Q.all(promises);
        }

        private collectUpdateModelPromises(): Q.Promise<any> {

            var promises: Q.Promise<any>[] = [];
            var controllerInstance: IBaseCheckoutController;
            var controllerOptions: IRegisterControlOptions;

            for (var controllerName in this.registeredControllers) {

                if (this.registeredControllers.hasOwnProperty(controllerName)) {
                    controllerInstance = <IBaseCheckoutController>this.registeredControllers[controllerName];
                    promises.push(controllerInstance.getUpdateModelPromise());
                }
            }

            return Q.all(promises);
        }

        private handleError(reason: any): void {

            console.error('Unable to retrieve the cart for the checkout', reason);

            throw reason;
        }

        public setOrderConfirmationToCache(orderConfirmationviewModel : any) : void {

            this.cacheProvider.defaultCache.set(this.orderConfirmationCacheKey, orderConfirmationviewModel).done();
        }

        public getOrderConfirmationFromCache(): Q.Promise<any> {

            return this.cacheProvider.defaultCache.get<any>(this.orderConfirmationCacheKey);
        }

        public clearOrderConfirmationFromCache(): void {

             this.cacheProvider.defaultCache.clear(this.orderConfirmationCacheKey).done();
        }

        public setOrderToCache(orderConfirmationviewModel : any) : void {

            this.cacheProvider.defaultCache.set(this.orderCacheKey, orderConfirmationviewModel).done();
        }
    }
}
