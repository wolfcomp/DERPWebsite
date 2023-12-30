﻿/* Copyright (c) FFXIVQuickLauncher https://github.com/goatcorp/FFXIVQuickLauncher/blob/master/LICENSE
 *
 * Modified to fit the needs of the project.
 */

using PDPWebsite.Patching.ZiPatch.Chunk;
using PDPWebsite.Patching.ZiPatch.Util;

namespace PDPWebsite.Patching.ZiPatch
{
    public class ZiPatchFile : IDisposable
    {
        private static readonly uint[] zipatchMagic =
        {
            0x50495A91, 0x48435441, 0x0A1A0A0D
        };

        private readonly Stream _stream;


        /// <summary>
        /// Instantiates a ZiPatchFile from a Stream
        /// </summary>
        /// <param name="stream">Stream to a ZiPatch</param>
        public ZiPatchFile(Stream stream)
        {
            _stream = stream;

            var reader = new BinaryReader(stream);
            if (zipatchMagic.Any(magic => magic != reader.ReadUInt32()))
                throw new ZiPatchException();
        }

        /// <summary>
        /// Instantiates a ZiPatchFile from a file path
        /// </summary>
        /// <param name="filepath">Path to patch file</param>
        public static ZiPatchFile FromFileName(string filepath)
        {
            var stream = SqexFileStream.WaitForStream(filepath, FileMode.Open);
            return new ZiPatchFile(stream);
        }



        public IEnumerable<ZiPatchChunk> GetChunks()
        {
            ZiPatchChunk chunk;
            do
            {
                chunk = ZiPatchChunk.GetChunk(_stream);

                yield return chunk;
            } while (chunk.ChunkType != EndOfFileChunk.Type);
        }

        public void Dispose()
        {
            _stream?.Dispose();
        }
    }
}