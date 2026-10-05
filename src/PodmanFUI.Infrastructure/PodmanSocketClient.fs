namespace PodmanFUI.Infrastructure

open System
open System.IO
open System.Net.Http
open System.Net.Sockets
open System.Threading
open System.Threading.Tasks
open PodmanFUI.Domain
open PodmanFUI.Domain.Errors
open PodmanFUI.Domain.Models

/// Hiện thực hóa giao diện IPodmanSocketClient giao tiếp qua Unix Domain Socket
type PodmanSocketClient(socketPath: string, mode: SocketMode) =

    let handler =
        new SocketsHttpHandler(
            ConnectCallback =
                Func<SocketsHttpConnectionContext, CancellationToken, ValueTask<Stream>>(fun _cancellationToken context ->
                    let socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified)
                    let endpoint = new UnixDomainSocketEndPoint(socketPath)
                    task {
                        do! socket.ConnectAsync(endpoint, context)
                        return new NetworkStream(socket, ownsSocket = true) :> Stream
                    } |> ValueTask<Stream>
                ),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5.0)
        )

    let httpClient =
        new HttpClient(handler, disposeHandler = true)

    do
        httpClient.BaseAddress <- Uri("http://d/v4.0.0/libpod/")
        httpClient.Timeout <- TimeSpan.FromSeconds(10.0)

    interface IPodmanSocketClient with
        member _.GetSystemInfoAsync() =
            async {
                try
                    let! response = httpClient.GetAsync("info") |> Async.AwaitTask
                    if response.IsSuccessStatusCode then
                        let! rawJson = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                        return LibpodJsonParser.parseSystemInfo rawJson socketPath mode
                    else
                        let statusCode = int response.StatusCode
                        let reason = if String.IsNullOrWhiteSpace(response.ReasonPhrase) then "Unknown" else response.ReasonPhrase
                        return Error (HttpFailure (statusCode, reason))
                with
                | :? HttpRequestException as ex ->
                    match ex.InnerException with
                    | :? SocketException as sex when sex.SocketErrorCode = SocketError.AccessDenied ->
                        return Error (AccessDenied (socketPath, sprintf "Bị từ chối quyền truy cập socket: %s" sex.Message))
                    | _ ->
                        return Error (HttpFailure (0, ex.Message))
                | :? TaskCanceledException ->
                    return Error (Timeout "Kết nối tới Podman socket bị quá thời gian (Timeout 10s)")
                | ex ->
                    return Error (HttpFailure (0, ex.Message))
            }

    interface IDisposable with
        member _.Dispose() =
            httpClient.Dispose()
