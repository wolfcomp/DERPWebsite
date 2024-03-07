/* Copyright (c) FFXIVQuickLauncher https://github.com/goatcorp/FFXIVQuickLauncher/blob/master/LICENSE
 *
 * Modified to fit the needs of the project.
 */

using DERPWebsite.Patching.Util;
using DERPWebsite.Patching.ZiPatch.Util;

namespace DERPWebsite.Patching.ZiPatch.Chunk.SqpkCommand
{
    internal class SqpkPatchInfo : SqpkChunk
    {
        // This is a NOP on recent patcher versions
        public new static string Command = "X";

        // Don't know what this stuff is for
        public byte Status { get; protected set; }
        public byte Version { get; protected set; }
        public ulong InstallSize { get; protected set; }

        public SqpkPatchInfo(ChecksumBinaryReader reader, long offset, long size) : base(reader, offset, size) { }

        protected override void ReadChunk()
        {
            using var advanceAfter = new AdvanceOnDispose(Reader, Size);
            Status = Reader.ReadByte();
            Version = Reader.ReadByte();
            Reader.ReadByte(); // Alignment

            InstallSize = Reader.ReadUInt64BE();
        }

        public override string ToString()
        {
            return $"{Type}:{Command}:{Status}:{Version}:{InstallSize}";
        }
    }
}