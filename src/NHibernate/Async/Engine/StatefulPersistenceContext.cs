﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Text;
using NHibernate.Collection;
using NHibernate.Engine.Loading;
using NHibernate.Impl;
using NHibernate.Persister.Collection;
using NHibernate.Persister.Entity;
using NHibernate.Proxy;
using NHibernate.Util;

namespace NHibernate.Engine
{
	using System.Threading.Tasks;
	using System.Threading;
	public partial class StatefulPersistenceContext : IPersistenceContext, ISerializable, IDeserializationCallback
	{

		#region IPersistenceContext Members

		/// <summary>
		/// Get the current state of the entity as known to the underlying
		/// database, or null if there is no corresponding row
		/// </summary>
		public async Task<object[]> GetDatabaseSnapshotAsync(object id, IEntityPersister persister, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			EntityKey key = session.GenerateEntityKey(id, persister);
			object cached;
			if (entitySnapshotsByKey.TryGetValue(key, out cached))
			{
				return cached == NoRow ? null : (object[])cached;
			}
			else
			{
				object[] snapshot = await (persister.GetDatabaseSnapshotAsync(id, session, cancellationToken)).ConfigureAwait(false);
				entitySnapshotsByKey[key] = snapshot ?? NoRow;
				return snapshot;
			}
		}

		/// <summary>
		/// Get the values of the natural id fields as known to the underlying
		/// database, or null if the entity has no natural id or there is no
		/// corresponding row.
		/// </summary>
		public async Task<object[]> GetNaturalIdSnapshotAsync(object id, IEntityPersister persister, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (!persister.HasNaturalIdentifier)
			{
				return null;
			}

			// if the natural-id is marked as non-mutable, it is not retrieved during a
			// normal database-snapshot operation...
			int[] props = persister.NaturalIdentifierProperties;
			bool[] updateable = persister.PropertyUpdateability;
			bool allNatualIdPropsAreUpdateable = true;
			for (int i = 0; i < props.Length; i++)
			{
				if (!updateable[props[i]])
				{
					allNatualIdPropsAreUpdateable = false;
					break;
				}
			}

			if (allNatualIdPropsAreUpdateable)
			{
				// do this when all the properties are updateable since there is
				// a certain likelihood that the information will already be
				// snapshot-cached.
				object[] entitySnapshot = await (GetDatabaseSnapshotAsync(id, persister, cancellationToken)).ConfigureAwait(false);
				if (entitySnapshot == NoRow)
				{
					return null;
				}
				object[] naturalIdSnapshot = new object[props.Length];
				for (int i = 0; i < props.Length; i++)
				{
					naturalIdSnapshot[i] = entitySnapshot[props[i]];
				}
				return naturalIdSnapshot;
			}
			else
			{
				return await (persister.GetNaturalIdentifierSnapshotAsync(id, session, cancellationToken)).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Possibly unproxy the given reference and reassociate it with the current session.
		/// </summary>
		/// <param name="maybeProxy">The reference to be unproxied if it currently represents a proxy. </param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		/// <returns> The unproxied instance. </returns>
		public Task<object> UnproxyAndReassociateAsync(object maybeProxy, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{
				// TODO H3.2 Not ported
				//ElementWrapper wrapper = maybeProxy as ElementWrapper;
				//if (wrapper != null)
				//{
				//  maybeProxy = wrapper.Element;
				//}
				if (maybeProxy.IsProxy())
				{
					var proxy = maybeProxy as INHibernateProxy; 
					
					ILazyInitializer li = proxy.HibernateLazyInitializer;
					ReassociateProxy(li, proxy);
					return li.GetImplementationAsync(cancellationToken); //initialize + unwrap the object
				}
				return Task.FromResult<object>(maybeProxy);
			}
			catch (Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		/// <summary>
		/// Force initialization of all non-lazy collections encountered during
		/// the current two-phase load (actually, this is a no-op, unless this
		/// is the "outermost" load)
		/// </summary>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		public async Task InitializeNonLazyCollectionsAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (loadCounter == 0)
			{
				log.Debug("initializing non-lazy collections");
				//do this work only at the very highest level of the load
				loadCounter++; //don't let this method be called recursively
				try
				{
					while (nonlazyCollections.Count > 0)
					{
						//note that each iteration of the loop may add new elements
						IPersistentCollection tempObject = nonlazyCollections[nonlazyCollections.Count - 1];
						nonlazyCollections.RemoveAt(nonlazyCollections.Count - 1);
						await (tempObject.ForceInitializationAsync(cancellationToken)).ConfigureAwait(false);
					}
				}
				finally
				{
					loadCounter--;
					ClearNullProperties();
				}
			}
		}

		#endregion
	}
}
