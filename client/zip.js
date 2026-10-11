import { Inflate } from "fflate";
import { WorkspaceError, limits, normalizePath, isRelevant, checkCancelled, pause } from "./path.js";

const fail = message => { throw new WorkspaceError("invalid-zip", message); };
const decoder = new TextDecoder("utf-8", { fatal: true });
const crcTable = Uint32Array.from({ length: 256 }, (_, n) => {
  let value = n;
  for (let bit = 0; bit < 8; bit++) value = value & 1 ? 0xedb88320 ^ value >>> 1 : value >>> 1;
  return value >>> 0;
});
export function crc32(bytes, seed = 0xffffffff) {
  let crc = seed;
  for (const byte of bytes) crc = crcTable[(crc ^ byte) & 255] ^ crc >>> 8;
  return crc >>> 0;
}

export async function inspectZip(file, signal) {
  checkCancelled(signal);
  if (file.size < 22 || file.size > limits.zipBytes) fail("Choose a ZIP bundle smaller than 32 MiB.");
  const tailStart = Math.max(0, file.size - 65557);
  const tail = new Uint8Array(await file.slice(tailStart).arrayBuffer());
  const tailView = new DataView(tail.buffer);
  let end = -1;
  for (let cursor = tail.length - 22; cursor >= 0; cursor--) {
    if (tailView.getUint32(cursor, true) === 0x06054b50 &&
        cursor + 22 + tailView.getUint16(cursor + 20, true) === tail.length) { end = cursor; break; }
  }
  if (end < 0) fail("This ZIP bundle has no valid directory.");
  const count = tailView.getUint16(end + 10, true);
  const directoryBytes = tailView.getUint32(end + 12, true);
  const directoryOffset = tailView.getUint32(end + 16, true);
  if (tailView.getUint16(end + 4, true) || tailView.getUint16(end + 6, true) ||
      tailView.getUint16(end + 8, true) !== count || count === 65535 ||
      directoryBytes === 0xffffffff || directoryOffset === 0xffffffff)
    fail("Split and ZIP64 bundles are not supported. Import the folder instead.");
  if (count > limits.zipEntries || directoryBytes > limits.zipDirectoryBytes ||
      directoryOffset + directoryBytes !== tailStart + end)
    fail("This ZIP bundle exceeds its safe directory limits.");
  const central = new Uint8Array(await file.slice(directoryOffset, directoryOffset + directoryBytes).arrayBuffer());
  const view = new DataView(central.buffer);
  const entries = new Map(), names = new Set(), ranges = [];
  let cursor = 0, expanded = 0;
  for (let index = 0; index < count; index++) {
    checkCancelled(signal);
    if (cursor + 46 > central.length || view.getUint32(cursor, true) !== 0x02014b50) fail("This ZIP directory is damaged.");
    const flags = view.getUint16(cursor + 8, true);
    const method = view.getUint16(cursor + 10, true);
    const checksum = view.getUint32(cursor + 16, true);
    const compressed = view.getUint32(cursor + 20, true);
    const size = view.getUint32(cursor + 24, true);
    const nameLength = view.getUint16(cursor + 28, true), extraLength = view.getUint16(cursor + 30, true), commentLength = view.getUint16(cursor + 32, true);
    const disk = view.getUint16(cursor + 34, true), attributes = view.getUint32(cursor + 38, true), offset = view.getUint32(cursor + 42, true);
    const next = cursor + 46 + nameLength + extraLength + commentLength;
    if (next > central.length || !nameLength || nameLength > 1024 || disk || flags & 1 ||
        compressed === 0xffffffff || size === 0xffffffff || offset === 0xffffffff ||
        ![0, 8].includes(method) || (attributes >>> 16 & 0xf000) === 0xa000)
      fail("Encrypted, linked or unsupported ZIP entries cannot be imported.");
    const nameBytes = central.subarray(cursor + 46, cursor + 46 + nameLength);
    if (!(flags & 0x800) && nameBytes.some(byte => byte > 127)) fail("Use UTF-8 filenames in the documentation ZIP.");
    let name;
    try { name = decoder.decode(nameBytes); } catch { fail("A ZIP filename is not valid UTF-8."); }
    const directory = name.endsWith("/");
    const path = normalizePath(directory ? name.slice(0, -1) : name, true);
    if (names.has(path)) fail("This ZIP bundle contains duplicate paths.");
    names.add(path);
    expanded += size;
    if (size > limits.assetBytes || expanded > limits.zipExpanded || size > Math.max(1, compressed) * 150)
      fail("This ZIP bundle exceeds its safe expansion limits.");
    if (method === 0 && compressed !== size) fail("This ZIP bundle contains inconsistent file sizes.");
    if (offset + 30 > directoryOffset) fail("This ZIP bundle contains invalid data offsets.");
    const localBytes = new Uint8Array(await file.slice(offset, offset + 30).arrayBuffer());
    const local = new DataView(localBytes.buffer);
    if (local.getUint32(0, true) !== 0x04034b50 || local.getUint16(6, true) !== flags || local.getUint16(8, true) !== method)
      fail("This ZIP bundle contains inconsistent file headers.");
    if (!(flags & 8) && (local.getUint32(14, true) !== checksum || local.getUint32(18, true) !== compressed || local.getUint32(22, true) !== size))
      fail("This ZIP bundle contains inconsistent local file sizes or checksums.");
    const localNameLength = local.getUint16(26, true), localExtraLength = local.getUint16(28, true);
    const start = offset + 30 + localNameLength + localExtraLength;
    if (localNameLength !== nameLength || start + compressed > directoryOffset) fail("This ZIP bundle contains invalid file bounds.");
    const localName = new Uint8Array(await file.slice(offset + 30, offset + 30 + localNameLength).arrayBuffer());
    if (!nameBytes.every((byte, position) => byte === localName[position])) fail("The ZIP entry name does not match its directory.");
    ranges.push([offset, start + compressed]);
    if (!directory && isRelevant(path)) entries.set(path, { path, size, compressed, method, checksum, start });
    cursor = next;
    if (index % 20 === 0) await pause();
  }
  if (cursor !== central.length) fail("This ZIP directory has unexpected data.");
  ranges.sort((a, b) => a[0] - b[0]);
  for (let index = 1; index < ranges.length; index++)
    if (ranges[index][0] < ranges[index - 1][1]) fail("This ZIP bundle contains overlapping files.");
  return entries;
}

export async function extractEntry(file, entry, maximum, signal) {
  checkCancelled(signal);
  if (entry.size > maximum) throw new WorkspaceError("file-too-large", "This file exceeds the reading limit.");
  const chunks = [];
  let size = 0, crc = 0xffffffff;
  const receive = chunk => {
    size += chunk.length;
    if (size > entry.size || size > maximum) fail("This ZIP entry expands beyond its declared size.");
    crc = crc32(chunk, crc); chunks.push(chunk.slice());
  };
  const inflater = entry.method === 8 ? new Inflate(receive) : null;
  try {
    if (entry.compressed === 0 && entry.method === 8) fail("This compressed ZIP entry is empty or damaged.");
    for (let offset = 0; offset < entry.compressed; offset += 4096) {
      checkCancelled(signal);
      const end = Math.min(entry.compressed, offset + 4096);
      const input = new Uint8Array(await file.slice(entry.start + offset, entry.start + end).arrayBuffer());
      if (inflater) inflater.push(input, end === entry.compressed);
      else receive(input);
      await pause();
    }
  } catch (error) {
    if (error instanceof WorkspaceError) throw error;
    fail("This ZIP entry could not be decompressed.");
  }
  if (size !== entry.size || ((crc ^ 0xffffffff) >>> 0) !== entry.checksum) fail("This ZIP entry failed its integrity check.");
  return new Blob(chunks);
}
