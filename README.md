# C# SignalR Protobuf Protocol

## [Nuget package](https://www.nuget.org/packages/Spillman.SignalR.Protobuf)

## To use
Call `SignalRBuilderExtensions.AddProtobufProtocol` with the `Types` of the Protobuf messages that you wish to serialize. The types need to be specified up-front in order to know which type to use when deserializing. The server and client must supply the same types with the same indexes for deserializing to work properly.

### Client example
```
var client = new HubConnectionBuilder()
    ...
    .AddProtobufProtocol(
        new Dictionary<int, Type>
        {
            [0] = typeof(FirstProtoMessage),
            [1] = typeof(SecondProtoMessage)
        }
    )
    .Build();
```

### Server example
```
services
    .AddSignalR() // or AddAzureSignalR()
    .AddProtobufProtocol(
        new Dictionary<int, Type>
        {
            [0] = typeof(FirstProtoMessage),
            [1] = typeof(SecondProtoMessage)
        }
    );
```

## Supported
- Calls that have any number of Protobuf arguments
- `List<IMessage>` handling. Note that there is overhead for each item in the list though (up to 8 bytes), so if polymorphism isn't needed, use a Protobuf model with `repeated` instead.

## Not supported
- Methods that take in or return primitives
