import * as flatbuffers from 'flatbuffers';
import type { WireRootIdentifier } from '../e2e/.wire/curious-contraptions/wire.js';

/** Converts a schema WireRootIdentifier value (its four little-endian bytes) to its four-character identifier.
 * Total over uint32: any value converts. */
export function rootIdentifier(root: number): string {
    return String.fromCharCode(root & 0xff, (root >>> 8) & 0xff, (root >>> 16) & 0xff, (root >>> 24) & 0xff);
}

/** The one TypeScript frame check: the size prefix must cover the buffer exactly, then the identifier must match the
 * expected root. Returns the ByteBuffer positioned at the size-prefixed root. */
export function openFrame(bytes: Uint8Array, root: WireRootIdentifier): flatbuffers.ByteBuffer {
    const identifier = rootIdentifier(root);
    if (bytes.length < flatbuffers.SIZE_PREFIX_LENGTH) {
        throw new Error(`a ${identifier} buffer is shorter than its size prefix`);
    }
    const declared = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength).getUint32(0, true) + flatbuffers.SIZE_PREFIX_LENGTH;
    if (declared > bytes.length) {
        throw new Error(`a ${identifier} buffer is truncated: holds ${bytes.length} of its ${declared} bytes`);
    }
    if (declared < bytes.length) {
        throw new Error(`a ${identifier} buffer has ${bytes.length - declared} trailing bytes after its ${declared}`);
    }
    const bb = new flatbuffers.ByteBuffer(bytes);
    bb.setPosition(flatbuffers.SIZE_PREFIX_LENGTH);
    const headerLength = flatbuffers.SIZE_PREFIX_LENGTH + Int32Array.BYTES_PER_ELEMENT + flatbuffers.FILE_IDENTIFIER_LENGTH;
    if (bytes.length < headerLength || !bb.__has_identifier(identifier)) {
        throw new Error(`wrong identifier: not a ${identifier} buffer`);
    }
    return bb;
}
