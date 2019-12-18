﻿using Orckestra.Composer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orckestra.Composer.Cart.ViewModels
{
    public class CustomerPaymentMethodListViewModel : BaseViewModel
    {
        public CustomerPaymentMethodListViewModel()
        {
            SavedCreditCards = new List<SavedCreditCardPaymentMethodViewModel>();
        }
        public List<SavedCreditCardPaymentMethodViewModel> SavedCreditCards { get; set; }

        /// <summary>
        /// Url to add a credit card to the customer's wallet profile
        /// </summary>
        public string AddWalletUrl { get; set; }
    }
}
