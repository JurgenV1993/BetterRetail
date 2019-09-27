﻿using System;
using System.Linq;
using Orckestra.Composer.Providers;
using Orckestra.Composer.Search.Context;
using Orckestra.Composer.Search.Providers;
using Orckestra.Composer.Search.Providers.FacetPredicate;
using Orckestra.Composer.Utils;
using Orckestra.Overture;
using Orckestra.Overture.ServiceModel.Search;
using SearchFilter = Orckestra.Composer.Parameters.SearchFilter;

namespace Orckestra.Composer.Search.Factory
{
    public class FacetPredicateFactory : ProviderFactory<IFacetPredicateProvider>, IFacetPredicateFactory
    {
        protected IFacetPredicateProviderRegistry FacetPredicateProviderRegistry { get; }
        protected IFacetConfigurationContext FacetConfigContext { get; }

        public FacetPredicateFactory(IDependencyResolver dependencyResolver, IFacetPredicateProviderRegistry facetPredicateProviderRegistry, IFacetConfigurationContext facetConfigContext)
            : base(dependencyResolver)
        {
            if (facetPredicateProviderRegistry == null) { throw new ArgumentNullException("facetPredicateProviderRegistry"); }
            FacetPredicateProviderRegistry = facetPredicateProviderRegistry;
            FacetConfigContext = facetConfigContext;
        }

        /// <summary>
        /// Creates a new instance of <see cref="FacetPredicate"/> based on a <param name="filter"></param> object.
        /// </summary>
        public virtual FacetPredicate CreateFacetPredicate(SearchFilter filter)
        {
            if (filter == null) { throw new ArgumentNullException("filter"); }
            if (string.IsNullOrWhiteSpace(filter.Name)) { throw new ArgumentException(ArgumentNullMessageFormatter.FormatErrorMessage("Name"), "filter"); }

            var setting = FacetConfigContext.GetFacetSettings()
                    .FirstOrDefault(s => s.FieldName.Equals(filter.Name, StringComparison.OrdinalIgnoreCase));

            if (setting == null)
            {
                return null;
            }

            Type factoryType = FacetPredicateProviderRegistry.ResolveProviderType(setting.FacetType.ToString());

            var instance = GetProviderInstance(factoryType);

            return instance.CreateFacetPredicate(filter);
        }
    }
}