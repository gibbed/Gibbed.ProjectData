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
using Newtonsoft.Json;

namespace Gibbed.ProjectData
{
    public sealed class Project
    {
        public string Name { get; init; }
        public bool IsHidden { get; init; }
        public string InstallPath { get; init; }
        public string ListsPath { get; init; }

        internal List<string> Dependencies { get; }
        internal Dictionary<string, string> Settings { get; }
        internal Manager Manager { get; init; }

        private Project()
        {
            this.Dependencies = new List<string>();
            this.Settings = new Dictionary<string, string>();
        }

        internal static Project Load(string path, Manager manager)
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

            var project = new Project
            {
                Name = definition.Name,
                IsHidden = definition.IsHidden,
                InstallPath = InstallLocation.Get(parentPath, definition.InstallLocations),
                ListsPath = listsPath,
                Manager = manager,
            };
            project.Dependencies.AddRange(definition.Dependencies);
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

            foreach (var name in this.Dependencies)
            {
                if (this.Manager.TryGetProject(name, out var dependency) == false)
                {
                    continue;
                }
                LoadListsFrom(
                    dependency.ListsPath,
                    filter,
                    hasher,
                    modifier,
                    extra,
                    list);
            }

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
