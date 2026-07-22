using System;
using System.Buffers.Binary;
using System.IO;

namespace CarinaStudio.AppSuite.Media;

/// <summary>
/// Utility to normalize vertical metrics of OpenType font data to make its line metrics consistent with the default Latin font (Inter).
/// </summary>
internal static class FontVerticalMetricsNormalizer
{
    // Located table of OpenType font.
    record struct TableRecord(int DirectoryEntryOffset, int Offset, int Length);


    // Constants.
    const uint ChecksumAdjustmentMagic = 0xB1B0AFBAu;
    const double NormalizedAscentRatio = 1984.0 / 2048; // hhea ascender of Inter
    const double NormalizedDescentRatio = 494.0 / 2048; // absolute hhea descender of Inter
    const ushort UseTypoMetricsFlagMask = 0x0080; // USE_TYPO_METRICS bit of OS/2 fsSelection


    // Calculate checksum of given range of font data. The range is treated as padded with zeros to a multiple of 4 bytes.
    static uint CalculateChecksum(ReadOnlySpan<byte> fontData, int offset, int length)
    {
        // sum up complete 32-bit words
        var sum = 0u;
        var end = offset + length;
        while (offset + 4 <= end)
        {
            sum = unchecked(sum + BinaryPrimitives.ReadUInt32BigEndian(fontData[offset..]));
            offset += 4;
        }

        // sum up the tail word padded with zeros
        if (offset < end)
        {
            Span<byte> tailWord = stackalloc byte[4];
            fontData[offset..end].CopyTo(tailWord);
            sum = unchecked(sum + BinaryPrimitives.ReadUInt32BigEndian(tailWord));
        }
        return sum;
    }


    // Find record of table with given tag.
    static TableRecord? FindTable(ReadOnlySpan<byte> fontData, ReadOnlySpan<byte> tag)
    {
        // scan table directory
        var tableCount = BinaryPrimitives.ReadUInt16BigEndian(fontData[4..]);
        for (var i = 0; i < tableCount; ++i)
        {
            var entryOffset = 12 + (i << 4);
            if (fontData.Slice(entryOffset, 4).SequenceEqual(tag))
            {
                return new TableRecord(
                    entryOffset,
                    (int)BinaryPrimitives.ReadUInt32BigEndian(fontData[(entryOffset + 8)..]),
                    (int)BinaryPrimitives.ReadUInt32BigEndian(fontData[(entryOffset + 12)..]));
            }
        }
        return null;
    }


    /// <summary>
    /// Normalize vertical metrics of given OpenType font data in place.
    /// </summary>
    /// <param name="fontData">Font data. Only single-face fonts are supported.</param>
    public static void Normalize(byte[] fontData)
    {
        // check sfnt version
        var span = fontData.AsSpan();
        var sfntVersion = BinaryPrimitives.ReadUInt32BigEndian(span);
        if (sfntVersion != 0x00010000u && sfntVersion != 0x4F54544Fu /* 'OTTO' */)
            throw new InvalidDataException($"Unsupported sfnt version: 0x{sfntVersion:X8}.");

        // locate required tables
        var headTable = FindTable(span, "head"u8) ?? throw new InvalidDataException("Missing 'head' table in font data.");
        var hheaTable = FindTable(span, "hhea"u8) ?? throw new InvalidDataException("Missing 'hhea' table in font data.");
        var os2Table = FindTable(span, "OS/2"u8) ?? throw new InvalidDataException("Missing 'OS/2' table in font data.");

        // calculate normalized metrics in design units of the font
        var unitsPerEm = BinaryPrimitives.ReadUInt16BigEndian(span[(headTable.Offset + 18)..]);
        var ascent = (short)Math.Round(unitsPerEm * NormalizedAscentRatio);
        var descent = (short)Math.Round(unitsPerEm * NormalizedDescentRatio);

        // patch metrics in 'hhea' table which is used by CoreText/FreeType/HarfBuzz
        BinaryPrimitives.WriteInt16BigEndian(span[(hheaTable.Offset + 4)..], ascent); // ascender
        BinaryPrimitives.WriteInt16BigEndian(span[(hheaTable.Offset + 6)..], (short)-descent); // descender
        BinaryPrimitives.WriteInt16BigEndian(span[(hheaTable.Offset + 8)..], 0); // lineGap

        // patch typographic and Windows metrics in 'OS/2' table which are used by DirectWrite, and raise USE_TYPO_METRICS flag to make all consumers select the same metrics
        var fsSelection = BinaryPrimitives.ReadUInt16BigEndian(span[(os2Table.Offset + 62)..]);
        BinaryPrimitives.WriteUInt16BigEndian(span[(os2Table.Offset + 62)..], (ushort)(fsSelection | UseTypoMetricsFlagMask));
        BinaryPrimitives.WriteInt16BigEndian(span[(os2Table.Offset + 68)..], ascent); // sTypoAscender
        BinaryPrimitives.WriteInt16BigEndian(span[(os2Table.Offset + 70)..], (short)-descent); // sTypoDescender
        BinaryPrimitives.WriteInt16BigEndian(span[(os2Table.Offset + 72)..], 0); // sTypoLineGap
        BinaryPrimitives.WriteUInt16BigEndian(span[(os2Table.Offset + 74)..], (ushort)ascent); // usWinAscent
        BinaryPrimitives.WriteUInt16BigEndian(span[(os2Table.Offset + 76)..], (ushort)descent); // usWinDescent

        // update checksums of modified tables. Checksum of 'head' table is defined to be calculated with zero checkSumAdjustment.
        BinaryPrimitives.WriteUInt32BigEndian(span[(headTable.Offset + 8)..], 0); // checkSumAdjustment
        UpdateTableChecksum(span, headTable);
        UpdateTableChecksum(span, hheaTable);
        UpdateTableChecksum(span, os2Table);

        // update checksum adjustment of whole font
        var fontChecksum = CalculateChecksum(span, 0, fontData.Length);
        BinaryPrimitives.WriteUInt32BigEndian(span[(headTable.Offset + 8)..], unchecked(ChecksumAdjustmentMagic - fontChecksum));
    }


    // Recalculate checksum of given table and update its table directory entry.
    static void UpdateTableChecksum(Span<byte> fontData, TableRecord table) =>
        BinaryPrimitives.WriteUInt32BigEndian(fontData[(table.DirectoryEntryOffset + 4)..], CalculateChecksum(fontData, table.Offset, table.Length));
}
