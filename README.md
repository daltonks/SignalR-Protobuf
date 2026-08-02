# SignalR Protobuf Protocol

[![NuGet](https://img.shields.io/nuget/v/Spillman.SignalR.Protobuf?label=Spillman.SignalR.Protobuf)](https://www.nuget.org/packages/Spillman.SignalR.Protobuf)

A SignalR hub protocol that puts Protobuf on the wire in place of JSON or MessagePack.

Hub method arguments are [Google.Protobuf](https://www.nuget.org/packages/Google.Protobuf) messages and get sent in their binary form. If your models already live in `.proto` files, this saves you from translating them into something SignalR knows how to serialize, and it keeps messages small — which is worth it when you are sending a lot of them.

## Install

```
dotnet add package Spillman.SignalR.Protobuf
```

Both ends of the connection need the package, the generated Protobuf types, and the same type map.

## The type map

Deserializing a message means knowing which type to deserialize into. The wire format identifies types by an index rather than by name, so the indexes have to be agreed on ahead of time: **the server and the client must map the same index to the same type.**

Put the map somewhere both projects can see it:

```cs
public static readonly IReadOnlyDictionary<int, Type> ProtobufTypes = new Dictionary<int, Type>
{
    [0] = typeof(FirstProtoMessage),
    [1] = typeof(SecondProtoMessage)
};
```

To add a type, append it with a new index. Renumbering or reusing an index breaks any client still running the old map.

## Register it

Server:

```cs
services
    .AddSignalR() // or AddAzureSignalR()
    .AddProtobufProtocol(ProtobufTypes);
```

Client:

```cs
var client = new HubConnectionBuilder()
    .WithUrl("https://example.com/realtime")
    .AddProtobufProtocol(ProtobufTypes)
    .Build();
```

`AddProtobufProtocol` clears any other registered `IHubProtocol`, so the connection speaks Protobuf and nothing else.

## What you can send

- Hub methods taking any number of Protobuf arguments.
- `List<IMessage>`, when a call needs to carry a mix of message types. Every item costs up to 8 extra bytes for its type index and size, so if you don't need the polymorphism, a message with a `repeated` field is cheaper.
- Messages whose type the receiver doesn't have in its map. Those arrive as `null` items rather than throwing, so a client on an older map can still handle the calls it does understand.

What you can't send:

- Methods that take or return primitives — `int`, `string`, and friends. Wrap them in a Protobuf message.

## Wire format

Each message is laid out as:

| Bytes | Contents |
| --- | --- |
| 4 | Total message size |
| 4 | Size of the metadata block |
| *n* | `MessageMetadata`, carrying the type index and byte size of every item |
| *n* | The items themselves, back to back |

Writes rent their buffer from `ArrayPool<byte>`, and reads seek past items whose type index isn't mapped instead of failing the whole message.

## Example

[`Example/`](Example) holds a server and a console client sharing a `.proto` file. Start the server, then the client, and they exchange a single message and a list of messages.

## Versions

| Package | Targets |
| --- | --- |
| 1.x (`master`) | .NET Standard 2.0, ASP.NET Core SignalR 3.1 |
| 2.x (`net10`) | .NET 10 |

## License

MIT.
