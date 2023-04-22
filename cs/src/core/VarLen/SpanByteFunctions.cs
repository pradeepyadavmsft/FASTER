﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Buffers;

namespace FASTER.core
{
    /// <summary>
    /// Callback functions for SpanByte key, value
    /// </summary>
    public class SpanByteFunctions<Key, Output, Context> : FunctionsBase<Key, SpanByte, SpanByte, Output, Context>
    {
        /// <inheritdoc />
        public override bool SingleWriter(ref Key key, ref SpanByte input, ref SpanByte src, ref SpanByte dst, ref Output output, ref UpsertInfo upsertInfo, WriteReason reason)
        {
            dst.UnmarkExtraMetadata();
            dst.ShrinkSerializedLength(src.Length);
            src.CopyTo(ref dst);
            return true;
        }

        /// <inheritdoc />
        public override bool ConcurrentWriter(ref Key key, ref SpanByte input, ref SpanByte src, ref SpanByte dst, ref Output output, ref UpsertInfo upsertInfo)
        {
            if (dst.Length < src.Length)
            {
                return false;
            }
            //Console.WriteLine($"Before {dst.Length} {src.Length}");


            // We can adjust the length header on the serialized log, if we wish.
            // This method will also zero out the extra space to retain log scan correctness.
            dst.UnmarkExtraMetadata();
            dst.ShrinkSerializedLength(src.Length);

            // Write the source data, leaving the destination size unchanged. You will need
            // to mange the actual space used by the value if you stop here.
            src.CopyTo(ref dst);
            //Console.WriteLine($"After {dst.Length} {src.Length}");
            return true;
        }

        /// <inheritdoc/>
        public override bool InitialUpdater(ref Key key, ref SpanByte input, ref SpanByte value, ref Output output, ref RMWInfo rmwInfo)
        {
            input.CopyTo(ref value);
            return true;
        }

        /// <inheritdoc/>
        public override bool CopyUpdater(ref Key key, ref SpanByte input, ref SpanByte oldValue, ref SpanByte newValue, ref Output output, ref RMWInfo rmwInfo)
        {
            oldValue.CopyTo(ref newValue);
            return true;
        }

        /// <inheritdoc/>
        public override bool InPlaceUpdater(ref Key key, ref SpanByte input, ref SpanByte value, ref Output output, ref RMWInfo rmwInfo)
        {
            // The default implementation of IPU simply writes input to destination, if there is space
            UpsertInfo upsertInfo = new(ref rmwInfo);
            return ConcurrentWriter(ref key, ref input, ref input, ref value, ref output, ref upsertInfo);
        }

        /// <inheritdoc/>
        /// <remarks>Avoids the "value = default" for added tombstone record, which do not have space for the payload</remarks>
        public override bool SingleDeleter(ref Key key, ref SpanByte value, ref DeleteInfo deleteInfo) => true;
    }

    /// <summary>
    /// Callback functions using SpanByteAndMemory output, for SpanByte key, value, input
    /// </summary>
    public class SpanByteFunctions<Context> : SpanByteFunctions<SpanByte, SpanByteAndMemory, Context>
    {
        private protected readonly MemoryPool<byte> memoryPool;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="memoryPool"></param>
        public SpanByteFunctions(MemoryPool<byte> memoryPool = default)
        {
            this.memoryPool = memoryPool ?? MemoryPool<byte>.Shared;
        }

        /// <inheritdoc />
        public unsafe override bool SingleReader(ref SpanByte key, ref SpanByte input, ref SpanByte value, ref SpanByteAndMemory dst, ref ReadInfo readInfo)
        {
            value.CopyTo(ref dst, memoryPool);
            return true;
        }

        /// <inheritdoc />
        public unsafe override bool ConcurrentReader(ref SpanByte key, ref SpanByte input, ref SpanByte value, ref SpanByteAndMemory dst, ref ReadInfo readInfo)
        {
            value.CopyTo(ref dst, memoryPool);
            return true;
        }
    }

    /// <summary>
    /// Callback functions for SpanByte with byte[] output, for SpanByte key, value, input
    /// </summary>
    public class SpanByteFunctions_ByteArrayOutput<Context> : SpanByteFunctions<SpanByte, byte[], Context>
    {
        /// <inheritdoc />
        public override bool SingleReader(ref SpanByte key, ref SpanByte input, ref SpanByte value, ref byte[] dst, ref ReadInfo readInfo)
        {
            dst = value.ToByteArray();
            return true;
        }

        /// <inheritdoc />
        public override bool ConcurrentReader(ref SpanByte key, ref SpanByte input, ref SpanByte value, ref byte[] dst, ref ReadInfo readInfo)
        {
            dst = value.ToByteArray();
            return true;
        }
    }
}
