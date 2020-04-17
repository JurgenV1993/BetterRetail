using Composite.Core;
using Orckestra.Composer.Configuration;
using Orckestra.Composer.SearchQuery.Parameters;
using Orckestra.Composer.SearchQuery.Repositories;
using Orckestra.ExperienceManagement.Configuration;
using Orckestra.Overture.ServiceModel.SearchQueries;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Orckestra.Composer.C1CMS.Queries
{
    public class QueryFacade
    {
        public static List<string> GetSearchQueryList(string type)
        {
            Enum.TryParse(type, out SearchQueryType queryType);

            try
            {
                var searchQueryRepository = ServiceLocator.GetService<ISearchQueryRepository>();
                var siteConfiguration = ServiceLocator.GetService<ISiteConfiguration>();
                var pageIdString = HttpContext.Current.Request["pageId"];
                if (Guid.TryParse(pageIdString, out Guid pageId))
                {
                    var scope = siteConfiguration.GetScopeIdByPageId(pageId);
                    var queries = searchQueryRepository.GetSearchQueriesAsync(new GetSearchQueriesParam()
                    {
                        Scope = scope,
                        QueryType = queryType
                    }).Result;

                    return queries.SearchQueries.Select(d => d.Name).ToList();
                }
            }
            catch
            {
                // ignored
            }

            return new List<string>();
        }
    }
}
