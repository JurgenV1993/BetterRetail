using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Orckestra.Composer.Parameters;
using Orckestra.Composer.Product.Parameters;
using Orckestra.Composer.Providers;
using Orckestra.Composer.Providers.Localization;
using Orckestra.Composer.Services;
using Orckestra.Composer.ViewModels;
using Orckestra.Composer.ViewModels.Breadcrumb;
using static Orckestra.Composer.Utils.MessagesHelper.ArgumentException;

namespace Orckestra.Composer.Product.Services
{
    public class ProductBreadcrumbService : IProductBreadcrumbService
    {
        protected ICategoryViewService CategoryViewService { get; private set; }
        protected ILocalizationProvider LocalizationProvider { get; private set; }
        protected ICategoryBrowsingUrlProvider CategoryBrowsingUrlProvider { get; private set; }

        public ProductBreadcrumbService(ICategoryViewService categoryViewService, ILocalizationProvider localizationProvider, ICategoryBrowsingUrlProvider categoryBrowsingUrlProvider)
        {
            CategoryViewService = categoryViewService ?? throw new ArgumentNullException(nameof(categoryViewService));
            LocalizationProvider = localizationProvider ?? throw new ArgumentNullException(nameof(localizationProvider));
            CategoryBrowsingUrlProvider = categoryBrowsingUrlProvider ?? throw new ArgumentNullException(nameof(categoryBrowsingUrlProvider));
        }


        /// <summary>
        /// Creates a <see cref="BreadcrumbViewModel"/> for a given product.
        /// </summary>
        /// <param name="parameters">Parameters to generate the ViewModel.</param>
        /// <returns></returns>
        public virtual async Task<BreadcrumbViewModel> CreateBreadcrumbAsync(GetProductBreadcrumbParam parameters)
        {
            AssertParameters(parameters);

            var categoriesPath = await GetCategoryViewModelsAsync(parameters.CategoryId, parameters.Scope, parameters.CultureInfo).ConfigureAwait(false);
            var vm = CreateBreadcrumbViewModel(parameters, categoriesPath);

            return vm;
        }

        protected virtual void AssertParameters(GetProductBreadcrumbParam parameters)
        {
            if (parameters == null) { throw new ArgumentNullException(nameof(parameters)); }
            if (parameters.CultureInfo == null) { throw new ArgumentException(GetMessageOfNull(nameof(parameters.CultureInfo)), nameof(parameters)); }

            if (!string.IsNullOrWhiteSpace(parameters.CategoryId) && string.IsNullOrWhiteSpace(parameters.Scope))
            {
                throw new ArgumentException($"{nameof(parameters.Scope)} must not be null or whitespace if {nameof(parameters.CategoryId)} is defined.", 
                    nameof(parameters));
            }
        }

        protected virtual Task<CategoryViewModel[]> GetCategoryViewModelsAsync(string categoryId, string scope, CultureInfo cultureInfo)
        {
            if (!string.IsNullOrEmpty(categoryId))
            {
                var task = CategoryViewService.GetCategoriesPathAsync(new GetCategoriesPathParam
                {
                    Scope = scope,
                    CultureInfo = cultureInfo,
                    CategoryId = categoryId
                });

                return task;
            }

            return Task.FromResult(new CategoryViewModel[0]);
        }

        protected virtual BreadcrumbViewModel CreateBreadcrumbViewModel(GetProductBreadcrumbParam parameters, IEnumerable<CategoryViewModel> categoriesPath)
        {
            var breadcrumbViewModel = new BreadcrumbViewModel
            {
                ActivePageName = parameters.ProductName
            };

            var stack = new Stack<BreadcrumbItemViewModel>();

            CreateBreadcrumbItemsForCategories(stack, GetCategoriesWithoutRoot(categoriesPath), parameters.CultureInfo, parameters.BaseUrl);
            CreateRootBreadcrumbItem(stack, parameters.HomeUrl, parameters.CultureInfo);

            UnrollStackIntoViewModel(stack, breadcrumbViewModel);

            return breadcrumbViewModel;
        }

        protected virtual CategoryViewModel[] GetCategoriesWithoutRoot(IEnumerable<CategoryViewModel> categories)
        {
            if (categories == null) { return null; }

            int nbCategories = categories.Count();
            return categories.TakeWhile((vm, i) => i < nbCategories - 1).ToArray();
        }

        protected virtual void CreateBreadcrumbItemsForCategories(Stack<BreadcrumbItemViewModel> stack, CategoryViewModel[] categories, CultureInfo cultureInfo, string baseUrl)
        {
            if (categories == null) { return; }

            foreach (var category in categories)
            {
                var url = CategoryBrowsingUrlProvider.BuildCategoryBrowsingUrl(new BuildCategoryBrowsingUrlParam()
                {
                    CultureInfo = cultureInfo,
                    CategoryId = category.Id,
                    IsAllProductsPage = false,
                    BaseUrl = baseUrl
                });
                var item = CreateBreadcrumbItem(category.DisplayName, url);
                stack.Push(item);
            }
        }

        protected virtual void CreateRootBreadcrumbItem(Stack<BreadcrumbItemViewModel> stack, string homeUrl, CultureInfo cultureInfo)
        {
            var item = CreateBreadcrumbItem(LocalizeString("General", "L_Home", cultureInfo), homeUrl);
            stack.Push(item);
        }

        protected virtual BreadcrumbItemViewModel CreateBreadcrumbItem(string displayName, string url)
        {
            return new BreadcrumbItemViewModel
            {
                DisplayName = displayName,
                Url = url
            };
        }

        protected virtual void UnrollStackIntoViewModel(Stack<BreadcrumbItemViewModel> stack, BreadcrumbViewModel breadcrumbViewModel)
        {
            while (stack.Count > 0)
            {
                var item = stack.Pop();
                breadcrumbViewModel.Items.Add(item);
            }
        }

        protected virtual string LocalizeString(string category, string key, CultureInfo cultureInfo)
        {
            return LocalizationProvider.GetLocalizedString(new GetLocalizedParam
            {
                Category = category,
                Key = key,
                CultureInfo = cultureInfo
            });
        }
    }
}
