using Prospect.Unreal.Serialization;

namespace Prospect.Unreal.Net.Packets.Header.Sequence;

public readonly struct SequenceHistory
{
    // Hardcoded values for FNetPacketNotify
    public const uint HistorySize = FNetPacketNotify.MaxSequenceHistoryLength;

    public const uint BitsPerWord = sizeof(uint) * 8;
    public const uint WordCount = HistorySize / BitsPerWord;
    public const uint MaxSizeInBits = WordCount * BitsPerWord;
    public const uint Size = HistorySize;

    private readonly uint[] _storage;

    public SequenceHistory()
    {
        _storage = new uint[WordCount];
    }
    
    public void Read(FBitReader reader, uint numWords)
    {
        numWords = Math.Min(numWords, WordCount);
        for (var i = 0; i < numWords; i++)
        {
            _storage[i] = reader.ReadUInt32();
        }
    }

    public void Reset()
    {
        for (var i = 0; i < _storage.Length; i++)
        {
            _storage[i] = 0;
        }
    }

    public bool IsDelivered(int index)
    {
        var wordIndex = (int)(index / BitsPerWord);
        var wordMask = 1 << (int)(index & (BitsPerWord - 1));

        return (_storage[wordIndex] & wordMask) != 0;
    }
}