namespace Prospect.Unreal.Net;

public record FChannelDefinition(string Name, Type Class, int StaticChannelIndex, bool TickOnCreate, bool ServerOpen, bool ClientOpen, bool InitialServer, bool InitialClient);