using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace OutOfWhatApp.Platform.MacOS;

// Raw Objective-C runtime bindings. Kept minimal and mechanical on purpose —
// this is the only file in the app that knows what libobjc looks like.
[SupportedOSPlatform("macos")]
internal static class AppKitInterop
{
    private const string ObjCLibrary = "/usr/lib/libobjc.A.dylib";

    [DllImport(ObjCLibrary)]
    public static extern IntPtr objc_getClass([MarshalAs(UnmanagedType.LPStr)] string name);

    [DllImport(ObjCLibrary)]
    public static extern IntPtr sel_registerName([MarshalAs(UnmanagedType.LPStr)] string name);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    public static extern IntPtr SendPtr(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    public static extern IntPtr SendPtr_Ptr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void SendVoid_Ptr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    public static extern IntPtr SendPtr_Double(IntPtr receiver, IntPtr selector, double arg1);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    public static extern IntPtr SendPtr_Ptr_UIntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, UIntPtr arg2);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    public static extern void SendVoid_NSSize(IntPtr receiver, IntPtr selector, NSSize arg1);

    // NSPoint is 16 bytes (two doubles) — small enough to return in registers
    // on both x86_64 and arm64, so plain objc_msgSend works (no _stret needed).
    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    public static extern NSPoint SendNSPoint(IntPtr receiver, IntPtr selector);

    // NSRect is 32 bytes — too large to return in registers on x86_64, which
    // requires the separate objc_msgSend_stret entry point for such calls.
    // arm64's ABI handles large struct returns transparently through the
    // normal objc_msgSend, so only Intel needs the _stret variant.
    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend_stret")]
    private static extern NSRect SendNSRect_Stret(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern NSRect SendNSRect_Direct(IntPtr receiver, IntPtr selector);

    public static NSRect SendNSRect(IntPtr receiver, IntPtr selector) =>
        RuntimeInformation.ProcessArchitecture == Architecture.X64
            ? SendNSRect_Stret(receiver, selector)
            : SendNSRect_Direct(receiver, selector);

    // Scalar double returns need objc_msgSend_fpret on x86_64 (float/double
    // results go through a different register convention there); arm64 is
    // fine with the normal entry point, same story as the NSRect split above.
    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend_fpret")]
    private static extern double SendDouble_FpRet(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern double SendDouble_Direct(IntPtr receiver, IntPtr selector);

    public static double SendDouble(IntPtr receiver, IntPtr selector) =>
        RuntimeInformation.ProcessArchitecture == Architecture.X64
            ? SendDouble_FpRet(receiver, selector)
            : SendDouble_Direct(receiver, selector);

    [DllImport(ObjCLibrary)]
    public static extern IntPtr objc_allocateClassPair(IntPtr superclass, [MarshalAs(UnmanagedType.LPStr)] string name, IntPtr extraBytes);

    [DllImport(ObjCLibrary)]
    public static extern void objc_registerClassPair(IntPtr cls);

    [DllImport(ObjCLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool class_addMethod(IntPtr cls, IntPtr selector, IntPtr impl, [MarshalAs(UnmanagedType.LPStr)] string types);
}

// Matches AppKit's NSSize (two CGFloats, i.e. two doubles on 64-bit).
[StructLayout(LayoutKind.Sequential)]
internal struct NSSize
{
    public double Width;
    public double Height;

    public NSSize(double width, double height)
    {
        Width = width;
        Height = height;
    }
}

// Matches AppKit's NSPoint (origin bottom-left of the main screen).
[StructLayout(LayoutKind.Sequential)]
internal struct NSPoint
{
    public double X;
    public double Y;
}

// Matches AppKit's NSRect (origin + size, origin bottom-left of the main screen).
[StructLayout(LayoutKind.Sequential)]
internal struct NSRect
{
    public NSPoint Origin;
    public NSSize Size;
}
