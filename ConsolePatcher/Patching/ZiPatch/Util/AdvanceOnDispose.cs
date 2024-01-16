/* Copyright (c) FFXIVQuickLauncher https://github.com/goatcorp/FFXIVQuickLauncher/blob/master/LICENSE
 *
 * Modified to fit the needs of the project.
 */

namespace PDPWebsite.Patching.ZiPatch.Util;

public sealed class AdvanceOnDispose : IDisposable
{
    private readonly Stream _stream;
    public readonly long OffsetBefore;
    public readonly long OffsetAfter;

    public AdvanceOnDispose(Stream stream, long size)
    {
        _stream = stream;
        OffsetBefore = _stream.Position;
        OffsetAfter = OffsetBefore + size;
    }

    public AdvanceOnDispose(BinaryReader reader, long size)
        : this(reader.BaseStream, size)
    {
    }

    public long NumBytesRemaining => OffsetAfter - _stream.Position;

    public void Dispose()
    {
        _stream.Position = OffsetAfter;
    }
}
