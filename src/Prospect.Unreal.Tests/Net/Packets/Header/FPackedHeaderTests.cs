using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using Prospect.Unreal.Net.Packets.Header;

namespace Prospect.Unreal.Tests.Net.Packets.Header;

[SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
public class FPackedHeaderTests
{
    [Test]
    [TestCase((uint)1221642592, (ushort)4660)]
    public void TestGetSeq(uint input, ushort output)
    {
        Assert.AreEqual(output, FPackedHeader.GetSeq(input).Value);
    }
    
    [Test]
    [TestCase((uint)1221642592, (ushort)3222)]
    public void TestGetAckedSeq(uint input, ushort output)
    {
        Assert.AreEqual(output, FPackedHeader.GetAckedSeq(input).Value);
    }
    
    [Test]
    [TestCase((uint)1221642592, (uint)0)]
    public void TestGetHistoryWordCount(uint input, uint output)
    {
        Assert.AreEqual(output, FPackedHeader.GetHistoryWordCount(input));
    }
}