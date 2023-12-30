﻿/* Copyright (c) FFXIVQuickLauncher https://github.com/goatcorp/FFXIVQuickLauncher/blob/master/LICENSE
 *
 * Modified to fit the needs of the project.
 */

using PDPWebsite.Patching.Util;
using PDPWebsite.Patching.ZiPatch.Util;

namespace PDPWebsite.Patching.ZiPatch.Chunk.SqpkCommand
{
    internal class SqpkFile : SqpkChunk
    {
        public new static string Command = "F";

        public enum OperationKind : byte
        {
            AddFile = (byte)'A',
            RemoveAll = (byte)'R',

            // I've seen no cases in the wild of these two
            DeleteFile = (byte)'D',
            MakeDirTree = (byte)'M'
        }

        public OperationKind Operation { get; protected set; }
        public long FileOffset { get; protected set; }
        public long FileSize { get; protected set; }
        public ushort ExpansionId { get; protected set; }
        public SqexFile TargetFile { get; protected set; }

        public List<long> CompressedDataSourceOffsets { get; protected set; }
        public List<SqpkCompressedBlock> CompressedData { get; protected set; }

        public SqpkFile(ChecksumBinaryReader reader, long offset, long size) : base(reader, offset, size) {}

        protected override void ReadChunk()
        {
            using var advanceAfter = new AdvanceOnDispose(Reader, Size);
            Operation = (OperationKind)Reader.ReadByte();
            Reader.ReadBytes(2); // Alignment

            FileOffset = Reader.ReadInt64BE();
            FileSize = Reader.ReadInt64BE();

            var pathLen = Reader.ReadUInt32BE();

            ExpansionId = Reader.ReadUInt16BE();
            Reader.ReadBytes(2);

            TargetFile = new SqexFile(Reader.ReadFixedLengthString(pathLen));

            if (Operation == OperationKind.AddFile)
            {
                CompressedDataSourceOffsets = new();
                CompressedData = new List<SqpkCompressedBlock>();

                while (advanceAfter.NumBytesRemaining > 0)
                {
                    CompressedDataSourceOffsets.Add(Offset + Reader.BaseStream.Position);
                    CompressedData.Add(new SqpkCompressedBlock(Reader));
                    CompressedDataSourceOffsets[CompressedDataSourceOffsets.Count - 1] += CompressedData[CompressedData.Count - 1].HeaderSize;
                }
            }
        }

        private static bool RemoveAllFilter(string filePath) =>
            !new[] { ".var", "00000.bk2", "00001.bk2", "00002.bk2", "00003.bk2" }.Any(filePath.EndsWith);

        public override void ApplyChunk(ZiPatchConfig config)
        {
            switch (Operation)
            {
                // Default behaviour falls through to AddFile, though this shouldn't happen
                case OperationKind.AddFile:
                default:
                    // TODO: Check this. I *think* boot usually creates all the folders like sqpack, movie, etc., so this might be kind of a hack
                    TargetFile.CreateDirectoryTree(config.GamePath);

                    var fileStream = config.Store == null ? TargetFile.OpenStream(config.GamePath, FileMode.OpenOrCreate) : TargetFile.OpenStream(config.Store, config.GamePath, FileMode.OpenOrCreate);

                    if (FileOffset == 0)
                        fileStream.SetLength(0);

                    fileStream.Seek(FileOffset, SeekOrigin.Begin);
                    foreach (var block in CompressedData)
                        block.DecompressInto(fileStream);

                    break;

                case OperationKind.RemoveAll:
                    foreach (var file in SqexFile.GetAllExpansionFiles(config.GamePath, ExpansionId).Where(RemoveAllFilter))
                        File.Delete(file);
                    break;

                case OperationKind.DeleteFile:
                    File.Delete(config.GamePath + "/" + TargetFile.RelativePath);
                    break;

                case OperationKind.MakeDirTree:
                    Directory.CreateDirectory(config.GamePath + "/" + TargetFile.RelativePath);
                    break;
            }
        }

        public override string ToString()
        {
            return $"{Type}:{Command}:{Operation}:{FileOffset}:{FileSize}:{ExpansionId}:{TargetFile}";
        }
    }
}