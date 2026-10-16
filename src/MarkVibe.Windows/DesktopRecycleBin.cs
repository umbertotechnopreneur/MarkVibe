/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; revision-checked Windows Recycle Bin operation; 2026-10-16
 * Human guidance: Umberto Giacobbi; file deletion requires the application's frosted confirmation
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
using System.Runtime.InteropServices;
using System.IO;
using System.Security.Cryptography;
using MarkVibe.Core;
using MarkVibe.Native;

namespace MarkVibe.Windows;

/// <summary>Recycles one confirmed workspace file, rejecting folders and changes made while its confirmation was open.</summary>
internal static class DesktopRecycleBin
{
    private static readonly Guid ShellItemId = new("43826D1E-E718-42EE-BC55-A1E261C37BFE");

    /// <summary>Captures the exact bytes and metadata of an existing file for a subsequent confirmation.</summary>
    public static string Revision(NativeFiles files, string workspaceId, string path)
    {
        var fullPath = files.Resolve(workspaceId, path);
        var file = new FileInfo(fullPath);
        if (!file.Exists || (file.Attributes & FileAttributes.Directory) != 0 || file.Length > 32L * 1048576)
        {
            throw new WorkspaceException("invalid-delete", "Choose an existing workspace file smaller than 32 MiB.");
        }
        using var content = File.OpenRead(fullPath);
        return file.Length + ":" + file.LastWriteTimeUtc.Ticks + ":" + Convert.ToHexString(SHA256.HashData(content));
    }

    /// <summary>Uses the Windows STA Shell operation with recycle-only flags and no secondary native prompts.</summary>
    public static void Delete(NativeFiles files, string workspaceId, string path, string expectedRevision)
    {
        if (Revision(files, workspaceId, path) != expectedRevision)
        {
            throw new WorkspaceException("file-changed", "The file changed while confirmation was open. Review it and try again.");
        }
        var fullPath = files.Resolve(workspaceId, path);
        var identifier = ShellItemId;
        Marshal.ThrowExceptionForHR(SHCreateItemFromParsingName(fullPath, IntPtr.Zero, ref identifier, out var item));
        var operation = (IFileOperation)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("3AD05575-8857-4850-9277-11B85BDB8E09"), true)!)!;
        try
        {
            // FOFX_RECYCLEONDELETE requests recycling; errors abort instead of offering a permanent-delete fallback.
            const uint silent = 0x0004, noConfirmation = 0x0010, noErrorUi = 0x0400;
            const uint recycleOnDelete = 0x00080000, earlyFailure = 0x00100000, addUndoRecord = 0x20000000;
            Marshal.ThrowExceptionForHR(operation.SetOperationFlags(silent | noConfirmation | noErrorUi | recycleOnDelete | earlyFailure | addUndoRecord));
            Marshal.ThrowExceptionForHR(operation.DeleteItem(item, IntPtr.Zero));
            Marshal.ThrowExceptionForHR(operation.PerformOperations());
            Marshal.ThrowExceptionForHR(operation.GetAnyOperationsAborted(out var aborted));
            if (aborted || File.Exists(fullPath))
            {
                throw new WorkspaceException("recycle-failed", "The file could not be moved to the Recycle Bin.");
            }
        }
        finally
        {
            Marshal.FinalReleaseComObject(operation);
            Marshal.FinalReleaseComObject(item);
        }
    }

    /// <summary>Creates the Shell item for a fully qualified, already-contained local file.</summary>
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int SHCreateItemFromParsingName(string path, IntPtr bindContext, ref Guid identifier, [MarshalAs(UnmanagedType.Interface)] out object item);

    /// <summary>Declares the native IFileOperation vtable; only flags, one-item delete and completion are used.</summary>
    [ComImport, Guid("947AAB5F-0A5C-4C13-B4D6-4BF7836FC9F8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOperation
    {
        /// <summary>Registers an optional progress sink.</summary>
        [PreserveSig] int Advise(IntPtr sink, out uint cookie);
        /// <summary>Unregisters a progress sink.</summary>
        [PreserveSig] int Unadvise(uint cookie);
        /// <summary>Sets Shell operation flags.</summary>
        [PreserveSig] int SetOperationFlags(uint flags);
        /// <summary>Sets an optional progress message.</summary>
        [PreserveSig] int SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)] string message);
        /// <summary>Sets an optional progress dialog.</summary>
        [PreserveSig] int SetProgressDialog(IntPtr dialog);
        /// <summary>Sets an optional property store.</summary>
        [PreserveSig] int SetProperties(IntPtr properties);
        /// <summary>Sets the owning native window.</summary>
        [PreserveSig] int SetOwnerWindow(IntPtr owner);
        /// <summary>Queues properties for one item.</summary>
        [PreserveSig] int ApplyPropertiesToItem(IntPtr item);
        /// <summary>Queues properties for multiple items.</summary>
        [PreserveSig] int ApplyPropertiesToItems(IntPtr items);
        /// <summary>Queues a one-item rename.</summary>
        [PreserveSig] int RenameItem(IntPtr item, IntPtr name, IntPtr sink);
        /// <summary>Queues a multiple-item rename.</summary>
        [PreserveSig] int RenameItems(IntPtr items, IntPtr name);
        /// <summary>Queues a one-item move.</summary>
        [PreserveSig] int MoveItem(IntPtr item, IntPtr destination, IntPtr name, IntPtr sink);
        /// <summary>Queues a multiple-item move.</summary>
        [PreserveSig] int MoveItems(IntPtr items, IntPtr destination);
        /// <summary>Queues a one-item copy.</summary>
        [PreserveSig] int CopyItem(IntPtr item, IntPtr destination, IntPtr name, IntPtr sink);
        /// <summary>Queues a multiple-item copy.</summary>
        [PreserveSig] int CopyItems(IntPtr items, IntPtr destination);
        /// <summary>Queues the single confirmed Shell item for deletion.</summary>
        [PreserveSig] int DeleteItem([MarshalAs(UnmanagedType.Interface)] object item, IntPtr sink);
        /// <summary>Queues multiple deletions; unused by the application.</summary>
        [PreserveSig] int DeleteItems(IntPtr items);
        /// <summary>Queues creation of a new item.</summary>
        [PreserveSig] int NewItem(IntPtr destination, uint attributes, IntPtr name, IntPtr template, IntPtr sink);
        /// <summary>Executes the queued single-file operation.</summary>
        [PreserveSig] int PerformOperations();
        /// <summary>Reports cancellation or a silently aborted operation.</summary>
        [PreserveSig] int GetAnyOperationsAborted([MarshalAs(UnmanagedType.Bool)] out bool aborted);
    }
}
