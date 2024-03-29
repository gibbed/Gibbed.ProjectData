﻿/* Copyright (c) 2021 Rick (rick 'at' gibbed 'dot' us)
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
using Newtonsoft.Json;

namespace Gibbed.ProjectData
{
    public sealed class Project
    {
        private Project()
        {
            this.Settings = new Dictionary<string, string>();
        }

        public string Name
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            private set;
#endif
        }

        public bool IsHidden
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            private set;
#endif
        }

        public string InstallPath
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            private set;
#endif
        }

        public string ListsPath
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            private set;
#endif
        }


        internal Dictionary<string, string> Settings { get; }

        public static Project Load(string path)
        {
            if (string.IsNullOrEmpty(path) == true)
            {
                throw new ArgumentNullException(nameof(path));
            }
            path = Path.GetFullPath(path);

            var definition = LoadJson<Definitions.ProjectDefinition>(path);

            var parentPath = Path.GetDirectoryName(path);

            var listsPath =
                string.IsNullOrEmpty(parentPath) == false &&
                Path.IsPathRooted(definition.ListsPath) == false
                    ? Path.Combine(parentPath, definition.ListsPath)
                    : Path.GetFullPath(definition.ListsPath);

            var project = new Project()
            {
                Name = definition.Name,
                IsHidden = definition.IsHidden,
                InstallPath = InstallLocation.Get(parentPath, definition.InstallLocations),
                ListsPath = listsPath,
            };
            foreach (var kv in definition.Settings)
            {
                project.Settings.Add(kv.Key, kv.Value);
            }
            return project;
        }

        private static TType LoadJson<TType>(string path)
        {
            using var stream = File.OpenRead(path);
            using var streamReader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(streamReader);
            var jsonSettings = new JsonSerializerSettings()
            {
                MissingMemberHandling = MissingMemberHandling.Error,
            };
            var jsonSerializer = JsonSerializer.CreateDefault(jsonSettings);
            return jsonSerializer.Deserialize<TType>(jsonReader);
        }

        public override string ToString()
        {
            return this.Name ?? base.ToString();
        }

        public string GetSetting(string name, string defaultValue)
        {
            if (string.IsNullOrEmpty(name) == true)
            {
                throw new ArgumentNullException(nameof(name));
            }
            return this.Settings.TryGetValue(name, out var value) == true
                ? value
                : defaultValue;
        }

        public TType GetSetting<TType>(string name, TType defaultValue)
            where TType : struct
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (this.Settings.TryGetValue(name, out var stringValue) == false)
            {
                return defaultValue;
            }

            var type = typeof(TType);
            if (type.IsEnum == true)
            {
                if (Enum.TryParse(stringValue, out TType enumValue) == true)
                {
                    return enumValue;
                }
                throw new ArgumentException("bad enum value", nameof(name));
            }
            return (TType)Convert.ChangeType(stringValue, type);
        }

        #region LoadLists
        public HashList<TType> LoadLists<TType>(string filter, Func<string, TType> hasher)
        {
            return this.LoadLists(filter, hasher, null, null);
        }

        public HashList<TType> LoadLists<TType>(string filter, Func<string, TType> hasher, Func<string, string> modifier)
        {
            return this.LoadLists(filter, hasher, modifier, null);
        }

        public HashList<TType> LoadLists<TType>(
            string filter,
            Func<string, TType> hasher,
            Func<string, string> modifier,
            Action<TType, string, string> extra)
        {
            var list = new HashList<TType>();
            LoadListsFrom(
                this.ListsPath,
                filter,
                hasher,
                modifier,
                extra,
                list);
            return list;
        }
        #endregion

        #region LoadListsFrom
        private static void LoadListsFrom<TType>(
            string basePath,
            string filter,
            Func<string, TType> hasher,
            Func<string, string> modifier,
            Action<TType, string, string> extra,
            HashList<TType> list)
        {
            if (Directory.Exists(basePath) == false)
            {
                return;
            }

            foreach (string listPath in Directory.GetFiles(basePath, filter, SearchOption.AllDirectories))
            {
                using var input = File.Open(listPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(input);
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    if (line.StartsWith(";") == true)
                    {
                        continue;
                    }

                    line = line.Trim();
                    if (line.Length <= 0)
                    {
                        continue;
                    }

                    string source = modifier == null ? line : modifier(line);
                    TType hash = hasher(source);

                    string otherSource;
                    if (list.Lookup.TryGetValue(hash, out otherSource) == true &&
                        otherSource != source)
                    {
                        throw new InvalidOperationException(
                            $"hash collision ('{source}' vs '{otherSource}')");
                    }

                    list.Lookup[hash] = source;

                    if (extra != null)
                    {
                        extra(hash, source, line);
                    }
                }
            }
        }
        #endregion
    }
}
