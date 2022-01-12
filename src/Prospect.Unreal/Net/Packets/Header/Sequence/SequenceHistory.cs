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

    public void Reset()
    {
        for (var i = 0; i < _storage.Length; i++)
        {
            _storage[i] = 0;
        }
    }

    public void AddDeliveryStatus(bool delivered)
    {
        var carry = delivered ? 1u : 0u;
        var valueMask = 1u << (int)(BitsPerWord - 1);

        for (var i = 0; i < WordCount; i++)
        {
            var oldValue = carry;

            carry = (_storage[i] & valueMask) >> (int)(BitsPerWord - 1);
            _storage[i] = (_storage[i] << 1) | oldValue;
        }
    }

    public bool IsDelivered(int index)
    {
        var wordIndex = (int)(index / BitsPerWord);
        var wordMask = 1 << (int)(index & (BitsPerWord - 1));

        return (_storage[wordIndex] & wordMask) != 0;
    }
    
    public void Read(FBitReader reader, uint numWords)
    {
        numWords = Math.Min(numWords, WordCount);
        for (var i = 0; i < numWords; i++)
        {
            _storage[i] = reader.ReadUInt32();
        }
    }

    public void Write(FBitWriter writer, uint numWords)
    {
        numWords = Math.Min(numWords, WordCount);
        for (var i = 0; i < numWords; i++)
        {
            writer.WriteUInt32(_storage[i]);
        }
    }
}