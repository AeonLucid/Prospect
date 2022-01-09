namespace Prospect.Unreal.Serialization;

public class FBitUtil
{
    public static readonly byte[] GShift = {0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80};

    public static readonly byte[] GMask = {0x00, 0x01, 0x03, 0x07, 0x0f, 0x1f, 0x3f, 0x7f};
    
    public static unsafe void AppBitsCpy(byte* dest, int destBit, byte* src, int srcBit, int bitCount)
    {
        if (bitCount == 0) return;

        // Special case - always at least one bit to copy,
        // a maximum of 2 bytes to read, 2 to write - only touch bytes that are actually used.
        if (bitCount <= 8)
        {
            int aDestIndex = destBit / 8;
            int aSrcIndex = srcBit / 8;
            int aLastDest = (destBit + bitCount - 1) / 8;
            int aLastSrc = (srcBit + bitCount - 1) / 8;
            int shiftSrc = srcBit & 7;
            int shiftDest = destBit & 7;
            int firstMask = 0xFF << shiftDest;
            int lastMask = 0xFE << ((destBit + bitCount - 1) & 7); // Pre-shifted left by 1.	
            int accu;

            if (aSrcIndex == aLastSrc)
                accu = (src[aSrcIndex] >> shiftSrc);
            else
                accu = ((src[aSrcIndex] >> shiftSrc) | (src[aLastSrc] << (8 - shiftSrc)));

            // One byte.
            if (aDestIndex == aLastDest)
            {
                int multiMask = firstMask & ~lastMask;
                dest[aDestIndex] = (byte) ((dest[aDestIndex] & ~multiMask) | ((accu << shiftDest) & multiMask));
            }
            // Two bytes.
            else
            {
                dest[aDestIndex] = (byte) ((dest[aDestIndex] & ~firstMask) | ((accu << shiftDest) & firstMask));
                dest[aLastDest] = (byte) ((dest[aLastDest] & lastMask) | ((accu >> (8 - shiftDest)) & ~lastMask));
            }

            return;
        }

        // Main copier, uses byte sized shifting. Minimum size is 9 bits, so at least 2 reads and 2 writes.
        int destIndex = destBit / 8;
        int firstSrcMask = 0xFF << (destBit & 7);
        int lastDest = (destBit + bitCount) / 8;
        int lastSrcMask = 0xFF << ((destBit + bitCount) & 7);
        int srcIndex = srcBit / 8;
        int lastSrc = (srcBit + bitCount) / 8;
        int shiftCount = (destBit & 7) - (srcBit & 7);
        int destLoop = lastDest - destIndex;
        int srcLoop = lastSrc - srcIndex;
        int fullLoop;
        int bitAccu;

        // Lead-in needs to read 1 or 2 source bytes depending on alignment.
        if (shiftCount >= 0)
        {
            fullLoop = Math.Max(destLoop, srcLoop);
            bitAccu = src[srcIndex] << shiftCount;
            shiftCount += 8; //prepare for the inner loop.
        }
        else
        {
            shiftCount += 8; // turn shifts -7..-1 into +1..+7
            fullLoop = Math.Max(destLoop, srcLoop - 1);
            bitAccu = src[srcIndex] << shiftCount;
            srcIndex++;
            shiftCount += 8; // Prepare for inner loop.  
            bitAccu = ((src[srcIndex] << shiftCount) + (bitAccu)) >> 8;
        }

        // Lead-in - first copy.
        dest[destIndex] = (byte)((bitAccu & firstSrcMask) | (dest[destIndex] & ~firstSrcMask));
        srcIndex++;
        destIndex++;

        // Fast inner loop. 
        for (; fullLoop > 1; fullLoop--)
        {   // ShiftCount ranges from 8 to 15 - all reads are relevant.
            bitAccu = ((src[srcIndex] << shiftCount) + (bitAccu)) >> 8; // Copy in the new, discard the old.
            srcIndex++;
            dest[destIndex] = (byte)bitAccu;  // Copy low 8 bits.
            destIndex++;
        }

        // Lead-out. 
        if (lastSrcMask != 0xFF)
        {
            if ((srcBit + bitCount - 1) / 8 == srcIndex) // Last legal byte ?
            {
                bitAccu = ((src[srcIndex] << shiftCount) + (bitAccu)) >> 8;
            }
            else
            {
                bitAccu = bitAccu >> 8;
            }

            dest[destIndex] = (byte)((dest[destIndex] & lastSrcMask) | (bitAccu & ~lastSrcMask));
        }
    }
}