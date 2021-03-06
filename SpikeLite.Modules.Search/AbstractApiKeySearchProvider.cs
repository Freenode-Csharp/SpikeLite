﻿/**
 * SpikeLite C# IRC Bot
 * Copyright (c) 2009-2013 FreeNode ##Csharp Community
 * 
 * This source is licensed under the terms of the MIT license. Please see the 
 * distributed license.txt for details.
 */

using System;
using System.Collections.Generic;

namespace SpikeLite.Modules.Search
{
    /// <summary>
    /// An abstract class for our search providers, allowing for some wiring and simple re-use.
    /// </summary>
    public abstract class AbstractApiKeySearchProvider : ISearchProvider
    {
        public string ApiKey { get; set; }

        public abstract void ExecuteSearch(string searchCriteria, ICollection<string> restrictToDomains, ICollection<string> excludeDomains, Action<string[]> callbackHandler);
    }
}
