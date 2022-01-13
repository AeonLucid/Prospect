using NUnit.Framework;
using Prospect.Unreal.Core;
using Prospect.Unreal.Serialization;

namespace Prospect.Unreal.Tests.Core;

public class FStringTests
{
    [Test]
    [TestCase("")]
    [TestCase("TestString")]
    [TestCase("/Game/ThirdPersonCPP/Maps/ThirdPersonExampleMap")]
    [TestCase("/Script/ThirdPersonMP.ThirdPersonMPGameMode")]
    public void TestWriteRead(string testValue)
    {
        // Write a string.
        using var writer = new FNetBitWriter(4096);
        
        FString.Serialize(writer, testValue);
        
        // Create a reader.
        var reader = new FNetBitReader(null, writer.GetData(), (int)writer.GetNumBits());
        var read = FString.Deserialize(reader);
        
        Assert.AreEqual(testValue, read);
    }
}