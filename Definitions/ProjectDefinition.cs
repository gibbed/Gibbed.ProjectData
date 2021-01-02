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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gibbed.ProjectData.Definitions
{
    internal class ProjectDefinition
    {
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty("list_location")]
        public string ListsPath { get; set; }

        [JsonProperty("hidden")]
        public bool IsHidden { get; set; }

        [JsonProperty("dependencies")]
        public List<string> Dependencies { get; }

        [JsonProperty("settings")]
        public Dictionary<string, string> Settings { get; }

        [JsonProperty("install_locations")]
        public List<InstallLocationDefinition> InstallLocations { get; }

        public ProjectDefinition()
        {
            this.Dependencies = new List<string>();
            this.Settings = new Dictionary<string, string>();
            this.InstallLocations = new List<InstallLocationDefinition>();
        }
    }
}
