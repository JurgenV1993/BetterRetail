using System;
using System.Collections.Generic;
using System.Linq;
using Orckestra.Composer.Cart.Parameters;
using Orckestra.Composer.Cart.ViewModels;
using static Orckestra.Composer.Utils.MessagesHelper.ArgumentException;

namespace Orckestra.Composer.Cart.Services
{
    public class CheckoutNavigationViewService : ICheckoutNavigationViewService
    {
        /// <summary>
        /// Get the view model for the checkout Navigation.
        /// </summary>
        /// <param name="param">param</param>
        /// <returns>The CheckoutNavigationViewModel</returns>
        public virtual CheckoutNavigationViewModel GetCheckoutNavigationViewModel(GetCheckoutNavigationParam param)
        {
            if (param == null) { throw new ArgumentNullException(nameof(param)); }
            if (param.StepUrls == null) { throw new ArgumentException(GetMessageOfNull(nameof(param.StepUrls)), nameof(param)); }

            var viewModel = new CheckoutNavigationViewModel
            {
                Steps = new List<CheckoutNavigationStepViewModel>()
            };

            foreach (var stepUrl in param.StepUrls.Where(x => x.Value.IsDisplayedInHeader))
            {
                var stepViewModel = new CheckoutNavigationStepViewModel
                {
                    Url = stepUrl.Value.Url,
                    Title = stepUrl.Value.Title,
                    IsActive = stepUrl.Key == param.CurrentStep,
                    IsEnable = stepUrl.Key < param.CurrentStep,
                    StepNumber = stepUrl.Key
                };

                viewModel.Steps.Add(stepViewModel);
            }
            viewModel.Context["Steps"] = viewModel.Steps;
            return viewModel;
        }

    }
}
