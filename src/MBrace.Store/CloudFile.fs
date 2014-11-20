﻿namespace Nessos.MBrace.Store

open System
open System.IO
open System.Runtime.Serialization

open Nessos.MBrace

[<Sealed; AutoSerializable(true)>]
type CloudFile =
    
    [<NonSerialized>]
    val mutable private provider : ICloudFileProvider
    val private providerId : string
    val private path : string

    internal new (provider : ICloudFileProvider, path : string) =
        {
            provider = provider
            providerId = ResourceRegistry<ICloudFileProvider>.GetId provider
            path = path
        }

    [<OnDeserializedAttribute>]
    member private __.OnDeserialized(_ : StreamingContext) =
        __.provider <- ResourceRegistry<ICloudFileProvider>.Resolve __.providerId

    member __.Path = __.path
    member __.Container = __.provider.GetFileContainer __.path
    member __.Name = __.provider.GetFileName __.path

    member __.ReadAsync () = __.provider.ReadFile __.path

    interface ICloudDisposable with
        member __.Dispose () = __.provider.DeleteFile __.path


and ICloudFileProvider =
    inherit IResource

    abstract GetFileContainer : path:string -> string
    abstract GetFileName : path:string -> string
    abstract IsValidPath : path:string -> bool
    abstract CreateUniqueContainerName : unit -> string
    abstract CreateUniqueFileName : ?container:string -> string

    /// <summary>
    ///     Checks if file exists in given path
    /// </summary>
    /// <param name="path">File path.</param>
    abstract FileExists : path:string -> Async<bool>

    /// <summary>
    ///     Deletes file in given path
    /// </summary>
    /// <param name="path">File path.</param>
    abstract DeleteFile : path:string -> Async<unit>

    /// <summary>
    ///     Gets all files that exist in given container
    /// </summary>
    /// <param name="path">Path to file container.</param>
    abstract EnumerateFiles : container:string -> Async<string []>

    /// <summary>
    ///     Checks if container exists in given path
    /// </summary>
    /// <param name="container">file container.</param>
    abstract ContainerExists : container:string -> Async<bool>
        
    /// <summary>
    ///     Deletes container in given path
    /// </summary>
    /// <param name="container">file container.</param>
    abstract DeleteContainer : container:string -> Async<unit>

    /// Get all container paths that exist in file system
    abstract EnumerateContainers   : unit -> Async<string []>

    /// <summary>
    ///     Creates a new file from provided stream.
    /// </summary>
    /// <param name="target">Target file.</param>
    /// <param name="source">Source stream.</param>
    abstract CopyFrom : target:string * source:Stream -> Async<unit>

    /// <summary>
    ///     Reads an existing file to target stream.
    /// </summary>
    /// <param name="source">Source file.</param>
    /// <param name="target">Target stream.</param>
    abstract CopyTo : source:string * target:Stream -> Async<unit>

    /// <summary>
    ///     Creates a new file in store.
    /// </summary>
    /// <param name="path">Path to new file.</param>
    abstract CreateFile : path:string -> Async<Stream>
    abstract ReadFile : path:string -> Async<Stream>