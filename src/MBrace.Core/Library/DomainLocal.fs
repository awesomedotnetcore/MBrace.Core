﻿namespace MBrace.Library

open System
open System.Collections.Concurrent
open System.Runtime.Serialization
open System.Threading

open MBrace.Core
open MBrace.Core.Internals

#nowarn "444"

/// A serializable value factory that will be initialized
/// exactly once in each AppDomain(Worker) that consumes it.
/// Distributed equivalent to System.Threading.ThreadLocal<T> type.
[<Sealed; DataContract>]
type DomainLocal<'T> internal (factory : unit -> 'T) =
    // Quoting from MSDN:
    // "For modifications and write operations to the dictionary, ConcurrentDictionary<TKey, TValue> uses fine-grained locking to ensure thread safety. 
    // However, delegates for these methods are called outside the locks to avoid the problems that can arise from executing unknown code under a lock. 
    // Therefore, the code executed by these delegates is not subject to the atomicity of the operation."
    //
    // We wrap the local value in a lazy value to ensure atomicity in factory execution as well.
    static let dict = new ConcurrentDictionary<string, Lazy<'T>>()

    [<DataMember(Name = "UUID")>]
    let id = mkUUID()

    [<DataMember(Name = "Factory")>]
    let factory = factory

    /// Gets whether value has been initialized in the current process.
    member __.IsValueCreated : bool = 
        dict.ContainsKey id // note that the presense of the key in the local dictionary
                            // is no indicator of the value having already been created.
                            // However, it does indicate that the value is currently being
                            // initialized by another thread which is good enough in this context.

    /// <summary>
    ///     Returns the value initialized in the local Application Domain.
    /// </summary>
    member __.Value : 'T = dict.GetOrAdd(id, lazy(factory ())).Value

/// A serializable value factory that will be initialized
/// exactly once in each AppDomain(Worker) that consumes it.
/// Distributed equivalent to System.Threading.ThreadLocal<T> type.
[<Sealed; DataContract>]
type DomainLocalMBrace<'T> internal (factory : LocalCloud<'T>) =
    // domain local value container
    static let dict = new ConcurrentDictionary<string, Lazy<'T>> ()

    [<DataMember(Name = "UUID")>]
    let id = mkUUID()

    [<DataMember(Name = "Factory")>]
    let factory = factory

    /// <summary>
    ///     Returns the value initialized in the local Application Domain.
    /// </summary>
    member __.Value : LocalCloud<'T> = local {
        let! ctx = Cloud.GetExecutionContext()
        let mkLazy _ = lazy (Cloud.RunSynchronously(factory, ctx.Resources, ctx.CancellationToken))
        let lv = dict.GetOrAdd(id, mkLazy)
        return lv.Value
    }

    /// Gets whether value has been initialized in the current process.
    member __.IsValueCreated : bool = 
        dict.ContainsKey id // note that the presense of the key in the local dictionary
                            // is no indicator of the value having already been created.
                            // However, it does indicate that the value is currently being
                            // initialized by another thread which is good enough in this context.

/// A serializable value factory that will be initialized
/// exactly once in each AppDomain(Worker) that consumes it.
/// Distributed equivalent to System.Threading.ThreadLocal<T> type.
type DomainLocal =

    /// <summary>
    ///     Creates a new DomainLocal entity with supplied factory.
    /// </summary>
    /// <param name="factory">Factory function.</param>
    static member Create(factory : unit -> 'T) = new DomainLocal<'T>(factory)

    /// <summary>
    ///     Creates a new DomainLocal entity with supplied MBrace factory workflow.
    /// </summary>
    /// <param name="factory">Factory workflow.</param>
    static member Create(factory : LocalCloud<'T>) = new DomainLocalMBrace<'T>(factory)