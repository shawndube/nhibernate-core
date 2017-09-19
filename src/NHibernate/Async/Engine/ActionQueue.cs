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
using System.Linq;
using System.Text;

using NHibernate.Action;
using NHibernate.Cache;
using NHibernate.Type;

namespace NHibernate.Engine
{
	using System.Threading.Tasks;
	using System.Threading;
	public partial class ActionQueue
	{

		public Task AddActionAsync(BulkOperationCleanupAction cleanupAction, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			return RegisterCleanupActionsAsync(cleanupAction, cancellationToken);
		}
	
		private async Task ExecuteActionsAsync(IList list, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			// Actions may raise events to which user code can react and cause changes to action list.
			// It will then fail here due to list being modified. (Some previous code was dodging the
			// trouble with a for loop which was not failing provided the list was not getting smaller.
			// But then it was clearing it without having executed added actions (if any), ...)
			foreach (IExecutable executable in list)
				await (ExecuteAsync(executable, cancellationToken)).ConfigureAwait(false);

			list.Clear();
			await (session.Batcher.ExecuteBatchAsync(cancellationToken)).ConfigureAwait(false);
		}

		public async Task ExecuteAsync(IExecutable executable, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				await (executable.ExecuteAsync(cancellationToken)).ConfigureAwait(false);
			}
			finally
			{
				await (RegisterCleanupActionsAsync(executable, cancellationToken)).ConfigureAwait(false);
			}
		}
		
		private async Task RegisterCleanupActionsAsync(IExecutable executable, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			beforeTransactionProcesses.Register(executable.BeforeTransactionCompletionProcess);
			if (session.Factory.Settings.IsQueryCacheEnabled)
			{
				string[] spaces = executable.PropertySpaces;
				afterTransactionProcesses.AddSpacesToInvalidate(spaces);
				await (session.Factory.UpdateTimestampsCache.PreInvalidateAsync(spaces, cancellationToken)).ConfigureAwait(false);
			}
			afterTransactionProcesses.Register(executable.AfterTransactionCompletionProcess);
		}

		/// <summary> 
		/// Perform all currently queued entity-insertion actions.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		public Task ExecuteInsertsAsync(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			return ExecuteActionsAsync(insertions, cancellationToken);
		}

		/// <summary> 
		/// Perform all currently queued actions. 
		/// </summary>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		public async Task ExecuteActionsAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await (ExecuteActionsAsync(insertions, cancellationToken)).ConfigureAwait(false);
			await (ExecuteActionsAsync(updates, cancellationToken)).ConfigureAwait(false);
			await (ExecuteActionsAsync(collectionRemovals, cancellationToken)).ConfigureAwait(false);
			await (ExecuteActionsAsync(collectionUpdates, cancellationToken)).ConfigureAwait(false);
			await (ExecuteActionsAsync(collectionCreations, cancellationToken)).ConfigureAwait(false);
			await (ExecuteActionsAsync(deletions, cancellationToken)).ConfigureAwait(false);
		}

		private async Task PrepareActionsAsync(IList queue, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			foreach (IExecutable executable in queue)
				await (executable.BeforeExecutionsAsync(cancellationToken)).ConfigureAwait(false);
		}

		/// <summary>
		/// Prepares the internal action queues for execution.  
		/// </summary>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		public async Task PrepareActionsAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await (PrepareActionsAsync(collectionRemovals, cancellationToken)).ConfigureAwait(false);
			await (PrepareActionsAsync(collectionUpdates, cancellationToken)).ConfigureAwait(false);
			await (PrepareActionsAsync(collectionCreations, cancellationToken)).ConfigureAwait(false);
		}
		
		/// <summary> 
		/// Performs cleanup of any held cache softlocks.
		/// </summary>
		/// <param name="success">Was the transaction successful.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		public Task AfterTransactionCompletionAsync(bool success, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			return afterTransactionProcesses.AfterTransactionCompletionAsync(success, cancellationToken);
		}
		private partial class AfterTransactionCompletionProcessQueue 
		{
	
			public async Task AfterTransactionCompletionAsync(bool success, CancellationToken cancellationToken) 
			{
				cancellationToken.ThrowIfCancellationRequested();
				int size = processes.Count;
				
				for (int i = 0; i < size; i++)
				{
					try
					{
						AfterTransactionCompletionProcessDelegate process = processes[i];
						process(success);
					}
					catch (CacheException e)
					{
						log.Error( "could not release a cache lock", e);
						// continue loop
					}
					catch (Exception e)
					{
						throw new AssertionFailure("Unable to perform AfterTransactionCompletion callback", e);
					}
				}
				processes.Clear();
	
				if (session.Factory.Settings.IsQueryCacheEnabled) 
				{
					await (session.Factory.UpdateTimestampsCache.InvalidateAsync(querySpacesToInvalidate.ToArray(), cancellationToken)).ConfigureAwait(false);
				}
				querySpacesToInvalidate.Clear();
			}
		}
	}
}
