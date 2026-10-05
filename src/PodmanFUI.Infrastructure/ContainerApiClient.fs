namespace PodmanFUI.Infrastructure

open System
open System.IO
open System.Net.Http
open System.Net.Sockets
open System.Threading
open System.Threading.Tasks
open PodmanFUI.Domain
open PodmanFUI.Domain.Errors
open PodmanFUI.Domain.ContainerModels

/// Client tương tác với Podman REST API qua Unix Domain Socket cho Container
type ContainerApiClient(socketPath: string) =

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
        httpClient.Timeout <- TimeSpan.FromSeconds(15.0)

    interface IContainerService with
        member _.ListContainersAsync() =
            async {
                try
                    let! response = httpClient.GetAsync("containers/json?all=true") |> Async.AwaitTask
                    if response.IsSuccessStatusCode then
                        let! rawJson = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                        return LibpodJsonParser.parseContainerSummaryList rawJson
                    else
                        let statusCode = int response.StatusCode
                        let reason = if String.IsNullOrWhiteSpace(response.ReasonPhrase) then "Unknown" else response.ReasonPhrase
                        return Error (HttpFailure (statusCode, sprintf "HTTP %d %s when listing containers" statusCode reason))
                with
                | :? HttpRequestException as ex ->
                    return Error (HttpFailure (0, sprintf "HTTP error connecting to socket: %s" ex.Message))
                | :? TaskCanceledException ->
                    return Error (Timeout "Connection to Podman socket timed out (15s)")
                | ex ->
                    return Error (HttpFailure (0, sprintf "Unexpected error: %s" ex.Message))
            }

        member _.GetContainerDetailAsync(containerId: string) =
            async {
                try
                    let uri = sprintf "containers/%s/json" (Uri.EscapeDataString(containerId))
                    let! response = httpClient.GetAsync(uri) |> Async.AwaitTask
                    if response.IsSuccessStatusCode then
                        let! rawJson = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                        return LibpodJsonParser.parseContainerDetail rawJson
                    else
                        let statusCode = int response.StatusCode
                        let reason = if String.IsNullOrWhiteSpace(response.ReasonPhrase) then "Unknown" else response.ReasonPhrase
                        return Error (HttpFailure (statusCode, sprintf "HTTP %d %s for container %s" statusCode reason containerId))
                with
                | :? HttpRequestException as ex ->
                    return Error (HttpFailure (0, sprintf "HTTP error: %s" ex.Message))
                | :? TaskCanceledException ->
                    return Error (Timeout "Connection to Podman socket timed out (15s)")
                | ex ->
                    return Error (HttpFailure (0, sprintf "Unexpected error: %s" ex.Message))
            }

        member _.PerformActionAsync (action: ContainerAction) (containerId: string) =
            async {
                try
                    let encodedId = Uri.EscapeDataString(containerId)
                    let! response =
                        match action with
                        | ContainerAction.Start ->
                            httpClient.PostAsync(sprintf "containers/%s/start" encodedId, null) |> Async.AwaitTask
                        | ContainerAction.Stop ->
                            httpClient.PostAsync(sprintf "containers/%s/stop?t=10" encodedId, null) |> Async.AwaitTask
                        | ContainerAction.Restart ->
                            httpClient.PostAsync(sprintf "containers/%s/restart?t=10" encodedId, null) |> Async.AwaitTask
                        | ContainerAction.Pause ->
                            httpClient.PostAsync(sprintf "containers/%s/pause" encodedId, null) |> Async.AwaitTask
                        | ContainerAction.Unpause ->
                            httpClient.PostAsync(sprintf "containers/%s/unpause" encodedId, null) |> Async.AwaitTask
                        | ContainerAction.Delete ->
                            httpClient.DeleteAsync(sprintf "containers/%s?force=false" encodedId) |> Async.AwaitTask
                        | ContainerAction.ExecShell ->
                            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NoContent)) |> Async.AwaitTask

                    if response.IsSuccessStatusCode then
                        return Ok ()
                    else
                        let statusCode = int response.StatusCode
                        let reason = if String.IsNullOrWhiteSpace(response.ReasonPhrase) then "Unknown" else response.ReasonPhrase
                        let! body = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                        let msg = if String.IsNullOrWhiteSpace(body) then reason else sprintf "%s - %s" reason body
                        return Error (HttpFailure (statusCode, msg))
                with
                | :? HttpRequestException as ex ->
                    return Error (HttpFailure (0, sprintf "HTTP error: %s" ex.Message))
                | :? TaskCanceledException ->
                    return Error (Timeout "Operation timed out")
                | ex ->
                    return Error (HttpFailure (0, sprintf "Unexpected error: %s" ex.Message))
            }

    interface IDisposable with
        member _.Dispose() =
            httpClient.Dispose()
