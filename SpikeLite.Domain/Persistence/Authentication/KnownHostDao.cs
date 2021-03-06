﻿/**
 * SpikeLite C# IRC Bot
 * Copyright (c) 2008-2011 FreeNode ##Csharp Community
 * 
 * This source is licensed under the terms of the MIT license. Please see the 
 * distributed license.txt for details.
 */

using System.Collections.Generic;
using SpikeLite.Domain.Model.Authentication;
using Spring.Data.NHibernate.Generic.Support;

// TODO: Kog 07/10/2009 - We should be able to do stuff like generic parameterless CRUD from either hibernate template or we need to
// TODO:                  to implement an abstract DAO that we can wrap it with.

namespace SpikeLite.Domain.Persistence.Authentication
{
    /// <summary>
    /// This class represents a cheap DAO to do DAO-like things upon known hosts (previously 'cloaks').
    /// We're not very exciting at present, but then our ORM handles all the CRUD ops that we used to
    /// hand-write.
    /// </summary>
    public class KnownHostDao : HibernateDaoSupport, IKnownHostDao
    {
        public virtual IList<KnownHost> FindAll()
        {
            return HibernateTemplate.ExecuteFind(x => x.CreateCriteria(typeof(KnownHost)).List<KnownHost>());
        }

        public void SaveOrUpdate(KnownHost host)
        {
            HibernateTemplate.SaveOrUpdate(host);
        }

        public void Delete(KnownHost host)
        {
            HibernateTemplate.Delete(host);
        }
    }
}