﻿
using Orckestra.Composer.Parameters;
using Orckestra.Composer.Requests;
using Orckestra.Composer.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orckestra.Composer.Services
{
    public interface IRecurringOrderTemplatesViewService
    {
        Task<bool> GetIsPaymentMethodUsedInRecurringOrders(GetIsPaymentMethodUsedInRecurringOrdersRequest request);

        Task<RecurringOrderTemplatesViewModel> GetRecurringOrderTemplatesViewModelAsync(GetRecurringOrderTemplatesParam param);
        Task<RecurringOrderTemplatesViewModel> UpdateRecurringOrderTemplateLineItemQuantityAsync(UpdateRecurringOrderTemplateLineItemQuantityParam param);
        Task<RecurringOrderTemplatesViewModel> RemoveRecurringOrderTemplateLineItemAsync(RemoveRecurringOrderTemplateLineItemParam request);
        Task<RecurringOrderTemplatesViewModel> RemoveRecurringOrderTemplatesLineItemsAsync(RemoveRecurringOrderTemplateLineItemsParam request);
        Task<RecurringOrderTemplatesViewModel> UpdateRecurringOrderTemplateLineItemAsync(UpdateRecurringOrderTemplateLineItemParam request);
        Task<RecurringOrderTemplateViewModel> GetRecurringOrderTemplateDetailViewModelAsync(GetRecurringOrderTemplateDetailParam param);
    }
}
