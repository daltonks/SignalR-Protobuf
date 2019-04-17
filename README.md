# ASP.NET Core SignalR Protobuf Protocol

## Note: I have yet to make a NuGet package for this, but it is in a working state.

## To use
Call `SignalRBuilderExtensions.AddProtobufProtocol` with the `MessageDescriptors` or `Types` of the Protobuf messages that you wish to serialize. The types need to be specified up-front in order to know which type to use when deserializing. The server and client must supply the same types in the same order for deserializing to work properly.

## Supported
- Calls that have any number of Protobuf arguments
- `List<IMessage>` handling. Note that there is overhead for each item in the list though (int type and byte size), so if polymorphism isn't needed, use a Protobuf model with `repeated` instead.

## Not supported at this time
- Methods that take in or return primitives
