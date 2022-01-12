using Prospect.Unreal.Core;
using Prospect.Unreal.Net;
using Prospect.Unreal.Net.Packets.Control;

[assembly: NetControlMessage("Hello", 0, typeof(byte), typeof(uint), typeof(FString))]
[assembly: NetControlMessage("Welcome", 1, typeof(FString), typeof(FString), typeof(FString))]
[assembly: NetControlMessage("Upgrade", 2, typeof(uint))]
[assembly: NetControlMessage("Challenge", 3, typeof(FString))]
[assembly: NetControlMessage("Netspeed", 4, typeof(int))]
[assembly: NetControlMessage("Login", 5, typeof(FString), typeof(FString), typeof(FUniqueNetIdRepl), typeof(FString))]
[assembly: NetControlMessage("Failure", 6, typeof(FString))]
[assembly: NetControlMessage("Join", 9)]
[assembly: NetControlMessage("JoinSplit", 10, typeof(FString), typeof(FUniqueNetIdRepl))]
[assembly: NetControlMessage("Skip", 12, typeof(FGuid))]
[assembly: NetControlMessage("Abort", 13, typeof(FGuid))]
[assembly: NetControlMessage("PCSwap", 15, typeof(int))]
[assembly: NetControlMessage("ActorChannelFailure", 16, typeof(int))]
[assembly: NetControlMessage("DebugText", 17, typeof(FString))]
[assembly: NetControlMessage("NetGUIDAssign", 18, typeof(FNetworkGUID), typeof(FString))]
[assembly: NetControlMessage("SecurityViolation", 19, typeof(FString))]
[assembly: NetControlMessage("GameSpecific", 20, typeof(byte), typeof(FString))]
[assembly: NetControlMessage("EncryptionAck", 21)]
[assembly: NetControlMessage("DestructionInfo", 22)]

[assembly: NetControlMessage("BeaconWelcome", 25)]
[assembly: NetControlMessage("BeaconJoin", 26, typeof(FString), typeof(FUniqueNetIdRepl))]
[assembly: NetControlMessage("BeaconAssignGUID", 27, typeof(FNetworkGUID))]
[assembly: NetControlMessage("BeaconNetGUIDAck", 28, typeof(FString))]
