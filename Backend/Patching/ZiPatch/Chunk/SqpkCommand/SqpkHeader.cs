/* Copyright (c) FFXIVQuickLauncher https://github.com/goatcorp/FFXIVQuickLauncher/blob/master/LICENSE
 *
 * Modified to fit the needs of the project.
 */

using PDPWebsite.Patching.Util;
using PDPWebsite.Patching.ZiPatch.Util;

namespace PDPWebsite.Patching.ZiPatch.Chunk.SqpkCommand
{
    class SqpkHeader : SqpkChunk
    {
        public new static string Command = "H";

        public enum TargetFileKind : byte
        {
            Dat = (byte)'D',
            Index = (byte)'I'
        }
        public enum TargetHeaderKind : byte
        {
            Version = (byte)'V',
            Index = (byte)'I',
            Data = (byte)'D'
        }

        public const int HEADER_SIZE = 1024;

        public TargetFileKind FileKind { get; protected set; }
        public TargetHeaderKind HeaderKind { get; protected set; }
        public SqpackFile TargetFile { get; protected set; }

        public byte[] HeaderData { get; protected set; }
        public long HeaderDataSourceOffset { get; protected set; }

        public SqpkHeader(ChecksumBinaryReader reader, long offset, long size) : base(reader, offset, size) {}

        protected override void ReadChunk()
        {
            using var advanceAfter = new AdvanceOnDispose(Reader, Size);
            FileKind = (TargetFileKind)Reader.ReadByte();
            HeaderKind = (TargetHeaderKind)Reader.ReadByte();
            Reader.ReadByte(); // Alignment

            if (FileKind == TargetFileKind.Dat)
                TargetFile = new SqpackDatFile(Reader);
            else
                TargetFile = new SqpackIndexFile(Reader);

            HeaderDataSourceOffset = Offset + Reader.BaseStream.Position;
            HeaderData = Reader.ReadBytes(HEADER_SIZE);
        }

        public override void ApplyChunk(ZiPatchConfig config, IProgress<float> progress)
        {
            TargetFile.ResolvePath(config.Platform);

            var file = config.Store == null ?
                TargetFile.OpenStream(config.GamePath, FileMode.OpenOrCreate) :
                TargetFile.OpenStream(config.Store, config.GamePath, FileMode.OpenOrCreate);

            file.WriteFromOffset(HeaderData, HeaderKind == TargetHeaderKind.Version ? 0 : HEADER_SIZE);
        }

        public override string ToString()
        {
            return $"{Type}:{Command}:{FileKind}:{HeaderKind}:{TargetFile}";
        }
    }
}