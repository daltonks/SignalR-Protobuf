# ASP.NET Core SignalR Protobuf Protocol

## Note: I have yet to make a NuGet package for this, but it is in a working state.

## To use
Call `SignalRBuilderExtensions.AddProtobufProtocol` with the `MessageDescriptors` or `Types` of the protobuf messages that you wish to serialize. The types need to be specified up-front in order to know which type to use when deserializing. The server and client must supply the same types in the same order for deserializing to work properly.
