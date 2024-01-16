/* Copyright (c) FFXIVQuickLauncher https://github.com/goatcorp/FFXIVQuickLauncher/blob/master/LICENSE
 *
 * Modified to fit the needs of the project.
 */

using PDPWebsite.Patching.Util;
using PDPWebsite.Patching.ZiPatch.Util;

namespace PDPWebsite.Patching.ZiPatch.Chunk
{
    public class AddDirectoryChunk : ZiPatchChunk
    {
        public new static string Type = "ADIR";

        public string DirName { get; protected set; }

        protected override void ReadChunk()
        {
            using var advanceAfter = new AdvanceOnDispose(Reader, Size);
            var dirNameLen = Reader.ReadUInt32BE();

            DirName = Reader.ReadFixedLengthString(dirNameLen);
        }


        public AddDirectoryChunk(ChecksumBinaryReader reader, long offset, long size) : base(reader, offset, size) {}

        public override void ApplyChunk(ZiPatchConfig config, IProgress<float> progress)
        {
            Directory.CreateDirectory(config.GamePath + DirName);
        }

        public override string ToString()
        {
            return $"{Type}:{DirName}";
        }
    }
}