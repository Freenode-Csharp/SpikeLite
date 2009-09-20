﻿/**
 * SpikeLite C# IRC Bot
 * Copyright (c) 2009 FreeNode ##Csharp Community
 * 
 * This source is licensed under the terms of the MIT license. Please see the 
 * distributed license.txt for details.
 */

using System;
using System.Collections.Generic;
using log4net.Ext.Trace;
using SpikeLite.Modules.Search.com.google.api;

namespace SpikeLite.Modules.Search
{
    /// <summary>
    /// Provides a Google Search based search provider.
    /// </summary>
    /// 
    /// <remarks>
    /// The Microsoft Live Search provider is preferred for two reasons: the first being that Google API keys are hard to come by, and the
    /// second being that Microsoft allows a higher number of searches per day. This provider is only used, by default, for the Google search
    /// itself.
    /// </remarks>
    public class GoogleSearchProvider : AbstractApiKeySearchProvider
    {
        /// <summary>
        /// This holds a reference to the web proxy we use for making search requests.
        /// </summary>
        private readonly GoogleSearchService _searchBroker;

        /// <summary>
        /// This holds a reference to a log4net instance that we can use for logging noteworthy schtuff.
        /// </summary>
        private readonly TraceLogImpl _logger = (TraceLogImpl)TraceLogManager.GetLogger(typeof(GoogleSearchProvider));

        public GoogleSearchProvider()
        {
            _searchBroker = new GoogleSearchService();
            _searchBroker.doGoogleSearchCompleted += SearchCompletedHandler;
        }

        public override void ExecuteSearch(string searchCriteria, string domain, Action<string[]> callbackHandler)
        {
            _searchBroker.doGoogleSearchAsync
                (
                ApiKey,
                searchCriteria,
                0,
                1,
                false,
                string.Empty,
                true,
                "lang_en",
                string.Empty,
                string.Empty,
                callbackHandler
                );
        }

        private void SearchCompletedHandler(object sender, doGoogleSearchCompletedEventArgs e)
        {
            List<string> results  = new List<string>();

            // Our Async finder may have thrown an exception, so let's catch it and spit it out at the user if we must.
            try
            {
                if (e.Result.resultElements.Length > 0)
                {
                    results.Add(String.Format("'{0}': {1} | {2}", 
                                              e.Result.searchQuery, 
                                              e.Result.resultElements[0].title.Replace("<b>", "").Replace(@"</b>", ""), 
                                              e.Result.resultElements[0].URL));                
                }
                else
                {
                    results.Add(String.Format("'{0}': No Results.", e.Result.searchQuery));    
                }
            }
            catch (Exception ex)
            {
                results.Add("The service is currently b00rked, please try again in a few minutes.");
                _logger.WarnFormat("Failed to execute search request, inner exception details: {0} - {1}", ex.InnerException.Message, ex.InnerException.StackTrace);
            }

            // Call back our callback from our container.
            ((Action<string[]>)e.UserState).Invoke(results.ToArray());
        }
             
    }
}
