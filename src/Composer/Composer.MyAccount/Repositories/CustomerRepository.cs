﻿using System;
using System.Threading.Tasks;
using Orckestra.Composer.Configuration;
using Orckestra.Composer.Exceptions;
using Orckestra.Composer.MyAccount.Parameters;
using Orckestra.Composer.Providers;
using Orckestra.Overture;
using Orckestra.Overture.Caching;
using Orckestra.Overture.ServiceModel.Customers;
using Orckestra.Overture.ServiceModel.Requests.Customers;
using Orckestra.Overture.ServiceModel.Requests.Customers.Membership;

namespace Orckestra.Composer.MyAccount.Repositories
{
    /// <summary>
    /// Repository for interacting with Customers
    /// Customers represents entities which have the ability to buy products. 
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        protected IOvertureClient OvertureClient { get; private set; }
        protected ICacheProvider CacheProvider { get; private set; }

        public CustomerRepository(IOvertureClient overtureClient, ICacheProvider cacheProvider)
        {
            if (overtureClient == null) { throw new ArgumentNullException("overtureClient"); }
            if (cacheProvider == null) { throw new ArgumentNullException("cacheProvider"); }

            OvertureClient = overtureClient;
            CacheProvider = cacheProvider;
        }

        /// <summary>
        /// Gets a single Customer based on it's unique identifier
        /// </summary>
        /// <getCustomerByIdParam name="getCustomerByIdParam">The Repository call params <see cref="GetCustomerByIdParam"/></getCustomerByIdParam>
        /// <returns>
        /// The Customer matching the requested ID, or null
        /// </returns>
        public virtual Task<Customer> GetCustomerByIdAsync(GetCustomerByIdParam getCustomerByIdParam)
        {
            if (getCustomerByIdParam == null) { throw new ArgumentNullException("getCustomerByIdParam"); }
            if (getCustomerByIdParam.CultureInfo == null) { throw new ArgumentException("getCustomerByIdParam.CultureInfo"); }
            if (string.IsNullOrWhiteSpace(getCustomerByIdParam.Scope)) { throw new ArgumentException("getCustomerByIdParam.Scope"); }
            if (getCustomerByIdParam.CustomerId == Guid.Empty) { throw new ArgumentException("getCustomerByIdParam.CustomerId"); }

            var cacheKey = new CacheKey(CacheConfigurationCategoryNames.Customer)
            {
                Scope = getCustomerByIdParam.Scope,
                CultureInfo = getCustomerByIdParam.CultureInfo,
            };

            cacheKey.AppendKeyParts(getCustomerByIdParam.CustomerId);

            var getCustomerByUsernameRequest = new GetCustomerRequest
            {
                IncludeAddresses = false,
                ScopeId = getCustomerByIdParam.Scope,
                CustomerId = getCustomerByIdParam.CustomerId,
            };

            return CacheProvider.GetOrAddAsync(cacheKey, () => OvertureClient.SendAsync(getCustomerByUsernameRequest));
        }

        /// <summary>
        /// Gets the single Customer identified by a password reset ticket
        /// </summary>
        /// <getCustomerByIdParam name="ticket">An encrypted password reset ticket</getCustomerByIdParam>
        /// <returns>
        /// The Customer matching the ticket and a status representing a possible cause of errors
        /// </returns>
        public virtual async Task<Customer> GetCustomerByTicketAsync(string ticket)
        {
            var request = new GetCustomerFromPasswordTicketRequest
            {
                Ticket = ticket
            };

            var customer = await OvertureClient.SendAsync(request).ConfigureAwait(false);

            return customer;
        }

        /// <summary>
        /// Create a new Customer
        /// </summary>
        /// <param name="createUserParam">The Repository call params <see cref="CreateUserParam"/></param>
        /// <returns>
        /// The created Customer
        /// </returns>
        public virtual async Task<Customer> CreateUserAsync(CreateUserParam createUserParam)
        {
            if (createUserParam == null) { throw new ArgumentNullException("createUserParam"); }
            if (createUserParam.CultureInfo == null) { throw new ArgumentException("createUserParam.CultureInfo"); }
            if (string.IsNullOrWhiteSpace(createUserParam.Password)) { throw new ArgumentException("createUserParam.Password"); }
            if (string.IsNullOrWhiteSpace(createUserParam.Email)) { throw new ArgumentException("createUserParam.Email"); }
            if (string.IsNullOrWhiteSpace(createUserParam.FirstName)) { throw new ArgumentException("createUserParam.FirstName"); }
            if (string.IsNullOrWhiteSpace(createUserParam.LastName)) { throw new ArgumentException("createUserParam.LastName"); }
            if (string.IsNullOrWhiteSpace(createUserParam.Scope)) { throw new ArgumentException("createUserParam.Scope"); }

            var request = new CreateCustomerMembershipRequest
            {
                Id = createUserParam.CustomerId,
                Username = createUserParam.Username,
                Email = createUserParam.Email,
                FirstName = createUserParam.FirstName,
                LastName = createUserParam.LastName,
                Password = createUserParam.Password,
                PasswordQuestion = createUserParam.PasswordQuestion,
                PasswordAnswer = createUserParam.PasswordAnswer,
                Language = createUserParam.CultureInfo.Name,
                ScopeId = createUserParam.Scope,
            };

            var createdCustomer = await OvertureClient.SendAsync(request).ConfigureAwait(false);

            return createdCustomer;
        }

        /// <summary>
        /// Update User infos
        /// </summary>
        /// <param name="param">The Repository call params <see cref="UpdateUserParam"/></param>
        /// <returns>
        /// The updated Customer and a status representing a possible cause of errors
        /// </returns>
        public virtual async Task<Customer> UpdateUserAsync(UpdateUserParam param)
        {
            if (param == null) { throw new ArgumentNullException("param"); }
            if (param.Customer == null) { throw new ArgumentException("param.Customer"); }

            var request = new UpdateCustomerRequest(param.Customer)
            {
                ScopeId = param.Scope
            };

            var updatedCustomer = await OvertureClient.SendAsync(request).ConfigureAwait(false);

            return updatedCustomer;
        }

        /// <summary>
        /// Sends instructions to the given email address on how to reset it's Customer's password
        /// </summary>
        /// <param name="param"></param>
        public virtual async Task SendResetPasswordInstructionsAsync(SendResetPasswordInstructionsParam param)
        {
            if (param == null) { throw new ArgumentNullException("param"); }
            if (string.IsNullOrWhiteSpace(param.Email)) { throw new ArgumentException("param.Email"); }
            if (string.IsNullOrWhiteSpace(param.Scope)) { throw new ArgumentException("param.Scope"); }

            var request = new ResetPasswordRequest
            {
                Email = param.Email,
                ScopeId = param.Scope
            };

            var response = await OvertureClient.SendAsync(request).ConfigureAwait(false);
            if (response.Success)
            {
                return;
            }

            throw new ComposerException(errorCode: "SendResetPasswordInstructionsFailed");
        }

        /// <summary>
        /// Resets the password for a customer
        /// This will change the password without actually needing the current password at all.
        /// and is part of the ForgotPassword process
        /// </summary>
        /// <getCustomerByIdParam name="username">The unique login Name of the customer to reset</getCustomerByIdParam>
        /// <getCustomerByIdParam name="newPassword">The new password to set</getCustomerByIdParam>
        /// <getCustomerByIdParam name="passwordAnswer">The answer to the password question</getCustomerByIdParam>
        public virtual async Task ResetPasswordAsync(string username, string scopeId, string newPassword, string passwordAnswer)
        {
            if (string.IsNullOrWhiteSpace(username)) { throw new ArgumentException("username"); }
            if (string.IsNullOrWhiteSpace(newPassword)) { throw new ArgumentException("newPassword"); }

            var request = new ResetPasswordRequest
            {
                Username = username,
                Password = newPassword,
                PasswordAnswer = passwordAnswer,
                ScopeId = scopeId
            };

            var response = await OvertureClient.SendAsync(request).ConfigureAwait(false);
            if (response.Success)
            {
                return;
            }

            throw new ComposerException(errorCode: "ResetPasswordFailed");
        }

        /// <summary>
        /// Sets the new password for a customer.
        /// </summary>
        /// <getCustomerByIdParam name="username">The unique login Name of the customer to update</getCustomerByIdParam>
        /// <getCustomerByIdParam name="oldPassword">The current login password of the customer to update</getCustomerByIdParam>
        /// <getCustomerByIdParam name="newPassword">The new password to set</getCustomerByIdParam>
        public virtual async Task ChangePasswordAsync(string username, string scopeId, string oldPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(username)) { throw new ArgumentException(nameof(username)); }
            if (string.IsNullOrWhiteSpace(oldPassword)) { throw new ArgumentException(nameof(oldPassword)); }
            if (string.IsNullOrWhiteSpace(newPassword)) { throw new ArgumentException(nameof(newPassword)); }
            if (string.IsNullOrWhiteSpace(scopeId)) { throw new ArgumentException(nameof(scopeId)); }

            var request = new ChangePasswordRequest
            {
                UserName = username,
                OldPassword = oldPassword,
                NewPassword = newPassword,
                ScopeId = scopeId
            };

            var response = await OvertureClient.SendAsync(request).ConfigureAwait(false);
            if (response.Success)
            {
                return;
            }

            throw new ComposerException(errorCode: "ChangePasswordFailed");
        }
    }
}
