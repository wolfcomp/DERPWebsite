/* Copyright (c) FFXIVQuickLauncher https://github.com/goatcorp/FFXIVQuickLauncher/blob/master/LICENSE
 *
 * Modified to fit the needs of the project.
 */

using DERPWebsite.Patching.Util;
using DERPWebsite.Patching.ZiPatch.Util;

namespace DERPWebsite.Patching.ZiPatch.Chunk
{
    public class DeleteDirectoryChunk : ZiPatchChunk
    {
        public new static string Type = "DELD";

        public string DirName { get; protected set; }

        public DeleteDirectoryChunk(ChecksumBinaryReader reader, long offset, long size) : base(reader, offset, size) { }

        protected override void ReadChunk()
        {
            using var advanceAfter = new AdvanceOnDispose(Reader, Size);
            var dirNameLen = Reader.ReadUInt32BE();

            DirName = Reader.ReadFixedLengthString(dirNameLen);
        }

        public override void ApplyChunk(ZiPatchConfig config, IProgress<float> progress)
        {
            Directory.Delete(config.GamePath + DirName);
        }

        public override string ToString()
        {
            return $"{Type}:{DirName}";
        }
    }
}