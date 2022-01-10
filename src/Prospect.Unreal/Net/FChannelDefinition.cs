using Prospect.Unreal.Core.Names;

namespace Prospect.Unreal.Net;

public record FChannelDefinition(FName Name, Type Class, int StaticChannelIndex, bool TickOnCreate, bool ServerOpen, bool ClientOpen, bool InitialServer, bool InitialClient);