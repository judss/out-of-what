using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static OutOfWhatApp.Platform.MacOS.AppKitInterop;

namespace OutOfWhatApp.Platform.MacOS;

// The only public surface the rest of the app is allowed to touch.
// Creates a real NSStatusItem whose button fires a single click callback —
// Avalonia's TrayIcon.Clicked never raises on macOS, so this exists to
// work around that platform gap without pulling AppKit into anything else.
[SupportedOSPlatform("macos")]
internal static class MacTrayIcon
{
    private static Action<double, double>? _onClick;
    private static IntPtr _statusItem;
    private static IntPtr _button;
    private static IntPtr _target;

    /// <summary>
    /// Creates the status item. <paramref name="onClick"/> receives the icon's own
    /// horizontal center (in AppKit's native screen space, origin bottom-left of the
    /// main screen) and the menu bar's thickness (points from the top of the screen
    /// down to the bottom of the menu bar) — both fixed, deterministic values,
    /// independent of where within the icon the user actually clicks.
    /// </summary>
    public static bool TryCreate(byte[] iconPngBytes, Action<double, double> onClick)
    {
        try
        {
            _onClick = onClick;

            var statusBarClass = objc_getClass("NSStatusBar");
            var systemStatusBar = SendPtr(statusBarClass, sel_registerName("systemStatusBar"));

            const double variableLength = -1.0; // NSVariableStatusItemLength
            _statusItem = SendPtr_Double(systemStatusBar, sel_registerName("statusItemWithLength:"), variableLength);
            if (_statusItem == IntPtr.Zero)
            {
                return false;
            }

            _button = SendPtr(_statusItem, sel_registerName("button"));
            if (_button == IntPtr.Zero)
            {
                return false;
            }

            var image = CreateImage(iconPngBytes);
            if (image != IntPtr.Zero)
            {
                SendVoid_Ptr(_button, sel_registerName("setImage:"), image);
            }

            _target = CreateClickTarget();
            SendVoid_Ptr(_button, sel_registerName("setTarget:"), _target);
            SendVoid_Ptr(_button, sel_registerName("setAction:"), sel_registerName("onTrayClick:"));

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IntPtr CreateImage(byte[] pngBytes)
    {
        var handle = GCHandle.Alloc(pngBytes, GCHandleType.Pinned);
        try
        {
            var dataClass = objc_getClass("NSData");
            var data = SendPtr_Ptr_UIntPtr(
                dataClass,
                sel_registerName("dataWithBytes:length:"),
                handle.AddrOfPinnedObject(),
                (UIntPtr)pngBytes.Length);

            var imageClass = objc_getClass("NSImage");
            var alloc = SendPtr(imageClass, sel_registerName("alloc"));
            var image = SendPtr_Ptr(alloc, sel_registerName("initWithData:"), data);

            // Menu bar icons need an explicit small size — NSImage won't shrink
            // to fit the status item on its own.
            const double menuBarIconSize = 18.0;
            SendVoid_NSSize(image, sel_registerName("setSize:"), new NSSize(menuBarIconSize, menuBarIconSize));

            return image;
        }
        finally
        {
            handle.Free();
        }
    }

    private static IntPtr CreateClickTarget()
    {
        var baseClass = objc_getClass("NSObject");
        var newClass = objc_allocateClassPair(baseClass, "OutOfWhatTrayTarget", IntPtr.Zero);

        unsafe
        {
            var imp = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr, void>)&OnTrayClickThunk;
            class_addMethod(newClass, sel_registerName("onTrayClick:"), imp, "v@:@");
        }

        objc_registerClassPair(newClass);

        var alloc = SendPtr(newClass, sel_registerName("alloc"));
        return SendPtr(alloc, sel_registerName("init"));
    }

    [UnmanagedCallersOnly]
    private static void OnTrayClickThunk(IntPtr self, IntPtr cmd, IntPtr sender)
    {
        var window = SendPtr(_button, sel_registerName("window"));
        var frame = SendNSRect(window, sel_registerName("frame"));
        var centerX = frame.Origin.X + frame.Size.Width / 2;

        // NSStatusItem's backing window frame includes extra invisible padding
        // beyond the visible menu bar, so it's not a reliable source for "how far
        // down is the bottom of the menu bar." NSStatusBar.thickness is the
        // documented, exact answer to that question.
        var statusBarClass = objc_getClass("NSStatusBar");
        var systemStatusBar = SendPtr(statusBarClass, sel_registerName("systemStatusBar"));
        var menuBarThickness = SendDouble(systemStatusBar, sel_registerName("thickness"));

        _onClick?.Invoke(centerX, menuBarThickness);
    }
}
