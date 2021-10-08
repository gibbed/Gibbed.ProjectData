/* Copyright (c) 2021 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using Gibbed.ProjectData.Definitions;
using Microsoft.Win32;

// CA1416 sucks and doesn't properly detect platform guards.

#pragma warning disable CA1416 // Validate platform compatibility

namespace Gibbed.ProjectData
{
    internal static class InstallLocation
    {
        public static string Get(string parentPath, List<InstallLocationDefinition> installLocations)
        {
            foreach (var installLocation in installLocations)
            {
                if (Get(parentPath, installLocation, out var installPath) == true)
                {
                    return installPath;
                }
            }
            return null;
        }

        public static bool Get(string initialPath, InstallLocationDefinition installLocation, out string path)
        {
            path = null;

            var currentPath = initialPath;
            foreach (var action in installLocation.Actions)
            {
                switch (action.Type)
                {
                    case "registry":
                    {
#if NET5_0_OR_GREATER
                        if (OperatingSystem.IsWindows() == false)
                        {
                            return false;
                        }
#endif
                        string value;
                        try
                        {
                            value = (string)Registry.GetValue(
                                action.RegistryKeyName, action.RegistryValueName, action.RegistryDefaultValue);
                            if (string.IsNullOrEmpty(value) == true)
                            {
                                return false;
                            }
                            value = CleanPath(value);
                        }
                        catch (SecurityException)
                        {
                            return false;
                        }
                        currentPath = value;
                        break;
                    }

                    case "registryview":
                    {
#if NET5_0_OR_GREATER
                        if (OperatingSystem.IsWindows() == false)
                        {
                            return false;
                        }
#endif
                        string value;
                        try
                        {
                            using var baseKey = RegistryKey.OpenBaseKey(action.RegistryHive, action.RegistryView);
                            using var subKey = baseKey.OpenSubKey(action.RegistrySubKeyName);
                            if (subKey == null)
                            {
                                return false;
                            }
                            value = (string)subKey.GetValue(action.RegistryValueName, action.RegistryDefaultValue);
                            if (string.IsNullOrEmpty(value) == true)
                            {
                                return false;
                            }
                            value = CleanPath(value);
                        }
                        catch (SecurityException)
                        {
                            return false;
                        }
                        currentPath = value;
                        break;
                    }

                    case "path":
                    {
                        var newPath = Path.GetFullPath(CleanPath(action.Value));
                        if (Directory.Exists(newPath) == false)
                        {
                            return false;
                        }
                        currentPath = newPath;
                        break;
                    }

                    case "combine":
                    {
                        var combinedPath = Path.Combine(currentPath, CleanPath(action.Value));
                        if (Directory.Exists(combinedPath) == false)
                        {
                            return false;
                        }
                        currentPath = combinedPath;
                        break;
                    }

                    case "parent":
                    {
                        var parentPath = Path.GetDirectoryName(currentPath);
                        if (string.IsNullOrEmpty(parentPath) == true ||
                            Directory.Exists(parentPath) == false)
                        {
                            return false;
                        }
                        currentPath = parentPath;
                        break;
                    }

                    default:
                    {
                        throw new InvalidOperationException("unhandled install location action type");
                    }
                }
            }

            path = currentPath;
            return true;
        }

        private static string CleanPath(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            return value
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
        }
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
