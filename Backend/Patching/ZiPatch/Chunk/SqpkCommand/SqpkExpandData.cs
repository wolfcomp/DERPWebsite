/* Copyright (c) FFXIVQuickLauncher https://github.com/goatcorp/FFXIVQuickLauncher/blob/master/LICENSE
 *
 * Modified to fit the needs of the project.
 */

using DERPWebsite.Patching.Util;
using DERPWebsite.Patching.ZiPatch.Util;

namespace DERPWebsite.Patching.ZiPatch.Chunk.SqpkCommand
{
    class SqpkExpandData : SqpkChunk
    {
        public new static string Command = "E";


        public SqpackDatFile TargetFile { get; protected set; }
        public long BlockOffset { get; protected set; }
        public long BlockNumber { get; protected set; }


        public SqpkExpandData(ChecksumBinaryReader reader, long offset, long size) : base(reader, offset, size) { }

        protected override void ReadChunk()
        {
            using var advanceAfter = new AdvanceOnDispose(Reader, Size);
            Reader.ReadBytes(3);

            TargetFile = new SqpackDatFile(Reader);

            BlockOffset = (long)Reader.ReadUInt32BE() << 7;
            BlockNumber = (long)Reader.ReadUInt32BE();

            Reader.ReadUInt32(); // Reserved
        }

        public override void ApplyChunk(ZiPatchConfig config, IProgress<float> progress)
        {
            TargetFile.ResolvePath(config.Platform);

            var file = config.Store == null ?
                TargetFile.OpenStream(config.GamePath, FileMode.OpenOrCreate) :
                TargetFile.OpenStream(config.Store, config.GamePath, FileMode.OpenOrCreate);

            SqpackDatFile.WriteEmptyFileBlockAt(file, BlockOffset, BlockNumber);
        }

        public override string ToString()
        {
            return $"{Type}:{Command}:{BlockOffset}:{BlockNumber}";
        }
    }
}