﻿using System.Runtime.InteropServices;
using UnrealSharp.Core;

namespace UnrealSharp.Plugins;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct PluginsCallbacks
{
    public delegate* unmanaged<char*, NativeBool, nint> LoadPlugin;
    public delegate* unmanaged<IntPtr, NativeBool> UnloadPlugin;
    
    [UnmanagedCallersOnly]
    private static nint ManagedLoadPlugin(char* assemblyPath, NativeBool isCollectible)
    {
        Plugin? newPlugin = PluginLoader.LoadPlugin(new string(assemblyPath), isCollectible.ToManagedBool());

        if (newPlugin == null || !newPlugin.IsAssemblyAlive)
        {
            return IntPtr.Zero;
        };

        return GCHandle.ToIntPtr(GCHandleUtilities.AllocateStrongPointer(newPlugin));
    }

    [UnmanagedCallersOnly]
    private static NativeBool ManagedUnloadPlugin(IntPtr pluginHandle)
    {
        Plugin? loadedAssembly = GCHandleUtilities.GetObjectFromHandlePtr<Plugin>(pluginHandle);
        
        if (loadedAssembly == null)
        {
            return NativeBool.False;
        }
        
        return PluginLoader.UnloadPlugin(loadedAssembly).ToNativeBool();
    }

    public static PluginsCallbacks Create()
    {
        return new PluginsCallbacks
        {
            LoadPlugin = &ManagedLoadPlugin,
            UnloadPlugin = &ManagedUnloadPlugin,
        };
    }
}