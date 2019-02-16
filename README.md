# ASP.NET Core SignalR Protobuf Protocol

## Note: I have yet to make a NuGet package for this, but it is in a working state.

## To use
Call `SignalRBuilderExtensions.AddProtobufProtocol` with the `MessageDescriptors` or `Types` of the Protobuf messages that you wish to serialize. The types need to be specified up-front in order to know which type to use when deserializing. The server and client must supply the same types in the same order for deserializing to work properly.

# Supported
- Methods that have any number of Protobuf arguments

## Not supported at this time
- Methods that take in or return primitives
- Enumerables of Protobuf messages in method arguments or return values (you will have to create a wrapper Protobuf message for this)
