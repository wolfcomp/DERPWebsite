/* Copyright (c) FFXIVQuickLauncher https://github.com/goatcorp/FFXIVQuickLauncher/blob/master/LICENSE
 *
 * Modified to fit the needs of the project.
 */

using PDPWebsite.Patching.Util;
using PDPWebsite.Patching.ZiPatch.Util;

namespace PDPWebsite.Patching.ZiPatch.Chunk
{
    // ReSharper disable once InconsistentNaming
    public class XXXXChunk : ZiPatchChunk
    {
        // TODO: This... Never happens.
        public new static string Type = "XXXX";

        protected override void ReadChunk()
        {
            using var advanceAfter = new AdvanceOnDispose(Reader, Size);
        }

        public XXXXChunk(ChecksumBinaryReader reader, long offset, long size) : base(reader, offset, size) {}

        public override string ToString()
        {
            return Type;
        }
    }
}