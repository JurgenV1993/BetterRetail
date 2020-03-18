﻿using System;
using System.Collections.Generic;
using System.Linq;
using Orckestra.Composer.Parameters;
using Orckestra.Composer.Search.Context;
using Orckestra.Composer.Search.Factory;
using Orckestra.Composer.Search.Parameters;
using Orckestra.Composer.Search.Repositories;
using Orckestra.Overture;
using Orckestra.Overture.ServiceModel.Queries;
using Orckestra.Overture.ServiceModel.Requests.Search;
using Orckestra.Overture.ServiceModel.Requests.SearchQueries;
using Orckestra.Overture.ServiceModel.Search;
using Orckestra.Overture.ServiceModel.SearchQueries;

namespace FeaturedProducts.Workaround
{
    public class WorkaroundProductRequestFactory: IProductRequestFactory
    {
        protected IOvertureClient OvertureClient { get; }
        protected IFacetPredicateFactory FacetPredicateFactory { get; }

        public WorkaroundProductRequestFactory(
            IOvertureClient overtureClient, IFacetPredicateFactory facetPredicateFactory)
        {
            OvertureClient = overtureClient ?? throw new ArgumentNullException(nameof(overtureClient));
            FacetPredicateFactory = facetPredicateFactory ?? throw new ArgumentNullException(nameof(facetPredicateFactory));
        }


        public virtual SearchAvailableProductsBaseRequest CreateProductRequest(SearchCriteria criteria)
        {
            var categoryCriteria = criteria as CategorySearchCriteria;
            if (categoryCriteria != null)
            {
                var featureProducts = GetFilteredFeaturedProducts(categoryCriteria);
                return new SearchAvailableProductsRequest()
                {
                    Query = CreateQuery(criteria.Scope),
                    FeaturedProducts = featureProducts
                };
                
            }

            return CreateProductRequest(criteria.Scope);
        }

        private string[] GetFilteredFeaturedProducts(CategorySearchCriteria categoryCriteria)
        {
            var searchQuery = OvertureClient.SendAsync(new GetSearchQueryByNameRequest()
                {
                    ScopeId = categoryCriteria.Scope,
                    Name = categoryCriteria.CategoryId,
                    QueryType = SearchQueryType.Category
                }

            ).Result;

            var featuredProducts = searchQuery?.QueryData.FirstOrDefault()?.ElevatedIds;

            if (featuredProducts == null || featuredProducts.Length == 0) return new string[0];

            var documentIdsExpression = " id:(" + String.Join(" OR ", featuredProducts.Select(x => $@"""{x}""")) + ")  AND Active:true ";

            var searchAvailableProductsRequest = new SearchAvailableProductsRequest
            {
                ScopeId = categoryCriteria.Scope,
                CultureName = categoryCriteria.CultureInfo.Name,
                Query = new Query
                {
                    MaximumItems = featuredProducts.Length,
                    StartingIndex = 0,
                    Filter = new FilterGroup
                    {
                        Filters = new List<Filter>
                        {
                            new Filter
                            {
                                Member = " ",
                                Operator = Operator.Custom,
                                CustomExpression = documentIdsExpression
                            }
                        }
                    }
                },
                IncludeFacets = categoryCriteria.IncludeFacets,
                FacetPredicates = BuildFacetPredicates(categoryCriteria),

                Properties = new List<string> { "id", "ProductId", "Active" }
            };
            try
            {
                var response = OvertureClient.SendAsync(searchAvailableProductsRequest).Result;

                if (response?.Documents?.Count > 0)
                {
                    // order products using the order of featuredProducts
                    return response.Documents.OrderBy(d => Array.FindIndex(featuredProducts, fi => fi == d.Id))
                        .ThenBy(x => x.Id).Select(x => x.Id).ToArray();
                }
            }
            catch (Exception ex)
            {
                var y = ex;
            }

            return new string[0];
        }

        protected virtual List<FacetPredicate> BuildFacetPredicates(SearchCriteria criteria)
        {
            var facetPredicates = new List<FacetPredicate>();

            if (criteria.SelectedFacets != null)
            {
                facetPredicates.AddRange(criteria.SelectedFacets
                    .Select(FacetPredicateFactory.CreateFacetPredicate)
                    .Where(fp => fp != null).ToList());
            }

            return facetPredicates;
        }

        /// <summary>
        /// Creates the query to see which products are active for a given catalog, i.e. scope.
        /// </summary>
        /// <param name="scopeId">The scope identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">scope id cannot be null or empty;scopeId</exception>
        public virtual SearchAvailableProductsRequest CreateProductRequest(string scopeId)
        {
            if (string.IsNullOrWhiteSpace(scopeId)) { throw new ArgumentException("scope id cannot be null or empty", "scopeId"); }

            var request = new SearchAvailableProductsRequest
            {
                Query = CreateQuery(scopeId)
            };

            return request;
        }

        protected virtual Query CreateQuery(string scopeId)
        {
            if (string.IsNullOrWhiteSpace(scopeId)) { throw new ArgumentException("scope id cannot be null or empty", "scopeId"); }

            return new Query
            {
                Filter = new FilterGroup
                {
                    BinaryOperator = BinaryOperator.And,
                    Filters = new List<Filter>
                    {
                        new Filter
                        {
                            Member = "CatalogId",
                            Operator = Operator.Equals,
                            Value = scopeId
                        },
                        new Filter
                        {
                            Member = "Active",
                            Operator = Operator.Equals,
                            Value= bool.TrueString
                        }
                    }
                }
            };
        }
    }
}
