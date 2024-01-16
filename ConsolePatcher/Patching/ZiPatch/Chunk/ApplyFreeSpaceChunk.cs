/* Copyright (c) FFXIVQuickLauncher https://github.com/goatcorp/FFXIVQuickLauncher/blob/master/LICENSE
 *
 * Modified to fit the needs of the project.
 */

using PDPWebsite.Patching.Util;
using PDPWebsite.Patching.ZiPatch.Util;

namespace PDPWebsite.Patching.ZiPatch.Chunk
{
    public class ApplyFreeSpaceChunk : ZiPatchChunk
    {
        // This is a NOP on recent patcher versions, so I don't think we'll be seeing it.
        public new static string Type = "APFS";

        // TODO: No samples of this were found, so these fields are theoretical
        public long UnknownFieldA { get; protected set; }
        public long UnknownFieldB { get; protected set; }

        protected override void ReadChunk()
        {
            using var advanceAfter = new AdvanceOnDispose(Reader, Size);
            UnknownFieldA = Reader.ReadInt64BE();
            UnknownFieldB = Reader.ReadInt64BE();
        }

        public ApplyFreeSpaceChunk(ChecksumBinaryReader reader, long offset, long size) : base(reader, offset, size) {}

        public override string ToString()
        {
            return $"{Type}:{UnknownFieldA}:{UnknownFieldB}";
        }
    }
}