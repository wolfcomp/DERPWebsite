/* Copyright (c) FFXIVQuickLauncher https://github.com/goatcorp/FFXIVQuickLauncher/blob/master/LICENSE
 *
 * Modified to fit the needs of the project.
 */

using DERPWebsite.Patching.Util;
using DERPWebsite.Patching.ZiPatch.Util;

namespace DERPWebsite.Patching.ZiPatch.Chunk
{
    public class EndOfFileChunk : ZiPatchChunk
    {
        public new static string Type = "EOF_";

        protected override void ReadChunk()
        {
            using var advanceAfter = new AdvanceOnDispose(Reader, Size);
        }

        public EndOfFileChunk(ChecksumBinaryReader reader, long offset, long size) : base(reader, offset, size) { }

        public override string ToString()
        {
            return Type;
        }
    }
}