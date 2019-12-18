﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orckestra.Composer.Cart.Factory.Order;
using Orckestra.Composer.Cart.Parameters;
using Orckestra.Composer.Cart.Parameters.Order;
using Orckestra.Composer.Cart.Repositories.Order;
using Orckestra.Composer.Cart.ViewModels.Order;
using Orckestra.Composer.Enums;
using Orckestra.Composer.Parameters;
using Orckestra.Composer.Providers;
using Orckestra.Composer.Providers.Dam;
using Orckestra.Composer.Services;
using Orckestra.Composer.Services.Lookup;
using Orckestra.Overture.ServiceModel.Orders;

namespace Orckestra.Composer.Cart.Services.Order
{
    public class OrderHistoryViewService : IOrderHistoryViewService
    {
        protected virtual IOrderHistoryViewModelFactory OrderHistoryViewModelFactory { get; private set; }
        protected virtual IOrderUrlProvider OrderUrlProvider { get; private set; }
        protected virtual ILookupService LookupService { get; private set; }
        protected virtual IOrderRepository OrderRepository { get; private set; }
        protected virtual IOrderDetailsViewModelFactory OrderDetailsViewModelFactory { get; private set; }
        protected virtual IImageService ImageService { get; private set; }
        protected virtual IShippingTrackingProviderFactory ShippingTrackingProviderFactory { get; private set; }

        public OrderHistoryViewService(IOrderHistoryViewModelFactory orderHistoryViewModelFactory,
            IOrderRepository orderRepository,
            IOrderUrlProvider orderUrlProvider,
            ILookupService lookupService,
            IOrderDetailsViewModelFactory orderDetailsViewModelFactory,
            IImageService imageService,
            IShippingTrackingProviderFactory shippingTrackingProviderFactory)
        {
            if (orderHistoryViewModelFactory == null) { throw new ArgumentNullException("orderHistoryViewModelFactory"); }
            if (orderUrlProvider == null) { throw new ArgumentNullException("orderUrlProvider"); }
            if (lookupService == null) { throw new ArgumentNullException("lookupService"); }
            if (orderRepository == null) { throw new ArgumentNullException("orderRepository"); }
            if (orderUrlProvider == null) { throw new ArgumentNullException("orderUrlProvider"); }
            if (orderDetailsViewModelFactory == null) { throw new ArgumentNullException("orderDetailsViewModelFactory"); }
            if (imageService == null) { throw new ArgumentNullException("imageService"); }
            if (shippingTrackingProviderFactory == null) { throw new ArgumentNullException("shippingTrackingProviderFactory"); }

            OrderHistoryViewModelFactory = orderHistoryViewModelFactory;
            OrderUrlProvider = orderUrlProvider;
            LookupService = lookupService;
            OrderRepository = orderRepository;
            OrderUrlProvider = orderUrlProvider;
            OrderDetailsViewModelFactory = orderDetailsViewModelFactory;
            ImageService = imageService;
            ShippingTrackingProviderFactory = shippingTrackingProviderFactory;
        }

        /// <summary>
        /// Gets the OrderHistory ViewModel, containing a list of Orders.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public virtual async Task<OrderHistoryViewModel> GetOrderHistoryViewModelAsync(GetCustomerOrdersParam param)
        {
            if (param == null) { throw new ArgumentNullException("param"); }
            if (param.CultureInfo == null) { throw new ArgumentException("param.CultureInfo"); }
            if (param.CustomerId == null) { throw new ArgumentException("param.CustomerId"); }
            if (string.IsNullOrWhiteSpace(param.Scope)) { throw new ArgumentException("param.Scope"); }

            var orderDetailBaseUrl = OrderUrlProvider.GetOrderDetailsBaseUrl(param.CultureInfo);

            var orderStatuses = await LookupService.GetLookupDisplayNamesAsync(new GetLookupDisplayNamesParam
            {
                CultureInfo = param.CultureInfo,
                LookupType = LookupType.Order,
                LookupName = "OrderStatus",
            }).ConfigureAwait(false);

            var orderQueryResult = await OrderRepository.GetCustomerOrdersAsync(param).ConfigureAwait(false);

            var shipmentsTrackingInfos = new Dictionary<Guid, TrackingInfoViewModel>();
            if (orderQueryResult != null && orderQueryResult.Results != null && param.OrderTense == OrderTense.CurrentOrders)
            {
                shipmentsTrackingInfos = await GetShipmentsTrackingInfoViewModels(orderQueryResult, param).ConfigureAwait(false);
            }

            var getOrderHistoryViewModelParam = new GetOrderHistoryViewModelParam
            {
                CultureInfo = param.CultureInfo,
                OrderResult = orderQueryResult,
                OrderStatuses = orderStatuses,
                Page = param.Page,
                OrderDetailBaseUrl = orderDetailBaseUrl,
                ShipmentsTrackingInfos = shipmentsTrackingInfos
            };

            var viewModel = OrderHistoryViewModelFactory.CreateViewModel(getOrderHistoryViewModelParam);

            return viewModel;
        }

        protected virtual async Task<Dictionary<Guid, TrackingInfoViewModel>> GetShipmentsTrackingInfoViewModels(OrderQueryResult orderQueryResult,
            GetCustomerOrdersParam param)
        {
            var shipmentsTrackingInfos = new Dictionary<Guid, TrackingInfoViewModel>();

            var getOrderTasks = orderQueryResult.Results.Select(order => OrderRepository.GetOrderAsync(new GetCustomerOrderParam
            {
                OrderNumber = order.OrderNumber,
                Scope = param.Scope
            }));

            var orders = await Task.WhenAll(getOrderTasks).ConfigureAwait(false);

            foreach (var order in orders)
            {
                foreach (var shipment in order.Cart.Shipments)
                {
                    var shippingTrackingProvider = ShippingTrackingProviderFactory.ResolveProvider(shipment.FulfillmentMethod.Name);
                    var trackingInfoVm = shippingTrackingProvider.GetTrackingInfoViewModel(shipment, param.CultureInfo);
                    shipmentsTrackingInfos.Add(shipment.Id, trackingInfoVm);
                }
            }

            return shipmentsTrackingInfos;
        }

        /// <summary>
        /// Gets an OrderDetailViewModel, containing all information about an order and his shipments.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public virtual async Task<OrderDetailViewModel> GetOrderDetailViewModelAsync(GetCustomerOrderParam param)
        {
            if (param == null) { throw new ArgumentNullException("param"); }
            if (param.CustomerId == null) { throw new ArgumentException("param.CustomerId"); }
            if (param.CultureInfo == null) { throw new ArgumentException("param.CultureInfo"); }
            if (string.IsNullOrWhiteSpace(param.Scope)) { throw new ArgumentException("param.Scope"); }
            if (string.IsNullOrWhiteSpace(param.OrderNumber)) { throw new ArgumentException("param.OrderNumber"); }
            if (string.IsNullOrWhiteSpace(param.CountryCode)) { throw new ArgumentException("param.CountryCode"); }
            if (string.IsNullOrWhiteSpace(param.BaseUrl)) { throw new ArgumentException("param.BaseUrl"); }

            var order = await OrderRepository.GetOrderAsync(param).ConfigureAwait(false);

            //Check if order is one of the current customer.
            if (order == null || Guid.Parse(order.CustomerId) != param.CustomerId)
            {
                return null;
            }

            var viewModel = await BuildOrderDetailViewModelAsync(order, param).ConfigureAwait(false);

            return viewModel;
        }

        /// <summary>
        /// Gets an OrderDetailViewModel for a guest customer, containing all information about an order and his shipments.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public virtual async Task<OrderDetailViewModel> GetOrderDetailViewModelForGuestAsync(GetOrderForGuestParam param)
        {
            if (param == null) { throw new ArgumentNullException("param"); }
            if (param.CultureInfo == null) { throw new ArgumentException("param.CultureInfo"); }
            if (string.IsNullOrWhiteSpace(param.Scope)) { throw new ArgumentException("param.Scope"); }
            if (string.IsNullOrWhiteSpace(param.OrderNumber)) { throw new ArgumentException("param.OrderNumber"); }
            if (string.IsNullOrWhiteSpace(param.CountryCode)) { throw new ArgumentException("param.CountryCode"); }
            if (string.IsNullOrWhiteSpace(param.BaseUrl)) { throw new ArgumentException("param.BaseUrl"); }
            if (string.IsNullOrWhiteSpace(param.Email)) { throw new ArgumentException("param.Email"); }

            var order = await OrderRepository.GetOrderAsync(param).ConfigureAwait(false);

            if (order == null || order.Cart.Customer.Email != param.Email)
            {
                return null;
            }

            var viewModel = await BuildOrderDetailViewModelAsync(order, param).ConfigureAwait(false);

            return viewModel;
        }

        protected virtual async Task<OrderDetailViewModel> BuildOrderDetailViewModelAsync(
            Overture.ServiceModel.Orders.Order order,
            GetOrderParam getOrderParam)
        {
            var shipmentsNotes = await GetShipmentsNotes(order.Cart.Shipments, getOrderParam.Scope).ConfigureAwait(false);
            var orderChanges = await OrderRepository.GetOrderChangesAsync(new GetOrderChangesParam{ OrderNumber = getOrderParam.OrderNumber, Scope = getOrderParam.Scope }).ConfigureAwait(false);

            var orderStatuses = await LookupService.GetLookupDisplayNamesAsync(new GetLookupDisplayNamesParam
            {
                CultureInfo = getOrderParam.CultureInfo,
                LookupType = LookupType.Order,
                LookupName = "OrderStatus",
            }).ConfigureAwait(false);

            var shipmentStatuses = await LookupService.GetLookupDisplayNamesAsync(new GetLookupDisplayNamesParam
            {
                CultureInfo = getOrderParam.CultureInfo,
                LookupType = LookupType.Order,
                LookupName = "ShipmentStatus",
            }).ConfigureAwait(false);

            var productImageInfo = new ProductImageInfo
            {
                ImageUrls = await ImageService.GetImageUrlsAsync(order.Cart.GetLineItems()).ConfigureAwait(false)
            };

            var viewModel = OrderDetailsViewModelFactory.CreateViewModel(new CreateOrderDetailViewModelParam
            {
                Order = order,
                OrderChanges = orderChanges,
                OrderStatuses = orderStatuses,
                ShipmentStatuses = shipmentStatuses,
                CultureInfo = getOrderParam.CultureInfo,
                CountryCode = getOrderParam.CountryCode,
                ProductImageInfo = productImageInfo,
                BaseUrl = getOrderParam.BaseUrl,
                ShipmentsNotes = shipmentsNotes
            });

            return viewModel;
        }

        protected virtual async Task<Dictionary<Guid, List<string>>> GetShipmentsNotes(List<Shipment> shipments, string scope)
        {
            var shipmentsNotes = new Dictionary<Guid, List<string>>();

            foreach (var shipmentId in shipments.Select(s => s.Id))
            {
                var notes = await OrderRepository.GetShipmentNotesAsync(new GetShipmentNotesParam
                {
                    ShipmentId = shipmentId,
                    Scope = scope
                }).ConfigureAwait(false);

                if (notes != null)
                {
                    shipmentsNotes.Add(shipmentId, notes.Select(n => n.Content).ToList());
                }
            }
            return shipmentsNotes;
        }
    }
}
