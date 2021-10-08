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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Gibbed.ProjectData
{
    public class Manager : IEnumerable<Project>
    {
        private string _BasePath;
        private readonly Dictionary<string, Project> _Projects;
        private Project _ActiveProject;

        private Manager()
        {
            this._Projects = new Dictionary<string, Project>();
        }

        public Project ActiveProject
        {
            get { return this._ActiveProject; }

            set
            {
                var currentPath = Path.Combine(this._BasePath, "current.txt");
                if (value == null)
                {
                    File.Delete(currentPath);
                }
                else
                {
                    File.WriteAllText(currentPath, value.Name, Encoding.UTF8);
                }
                this._ActiveProject = value;
            }
        }

        public Project this[string name]
        {
            get
            {
                if (string.IsNullOrEmpty(name) == true)
                {
                    throw new ArgumentNullException(nameof(name));
                }
                return this._Projects.TryGetValue(name, out var project) == false ? null : project;
            }
        }

        public bool TryGetProject(string name, out Project project)
        {
            if (string.IsNullOrEmpty(name) == true)
            {
                throw new ArgumentNullException(nameof(name));
            }
            return this._Projects.TryGetValue(name, out project);
        }

        public static Manager Load()
        {
            return Load(null);
        }

        public static Manager Load(string currentProject)
        {
            string basePath;
            basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            basePath = basePath != null ? Path.Combine(basePath, "projects") : "projects";
            return Load(basePath, currentProject);
        }

        public static Manager Load(string basePath, string currentProject)
        {
            if (string.IsNullOrEmpty(basePath) == true)
            {
                throw new ArgumentNullException(nameof(basePath));
            }

            if (string.IsNullOrEmpty(currentProject) == false)
            {
                currentProject = currentProject.Trim();
            }

            var manager = new Manager();
            manager._BasePath = basePath;

            var basePathExists = Directory.Exists(basePath);

            if (basePathExists == true)
            {
                foreach (string projectPath in Directory.GetFiles(basePath, "*.json", SearchOption.TopDirectoryOnly))
                {
                    var project = Project.Load(projectPath, manager);
                    manager._Projects.Add(project.Name, project);
                }
            }

            if (string.IsNullOrEmpty(currentProject) == false)
            {
                if (manager.TryGetProject(currentProject, out var activeProject) == true)
                {
                    manager._ActiveProject = activeProject;
                }
            }
            else if (basePathExists == true)
            {
                var currentPath = Path.Combine(basePath, "current.txt");
                if (File.Exists(currentPath) == true)
                {
                    currentProject = File.ReadAllText(currentPath).Trim();
                    if (manager.TryGetProject(currentProject, out var activeProject) == true)
                    {
                        manager._ActiveProject = activeProject;
                    }
                }
            }

            return manager;
        }

        public IEnumerator<Project> GetEnumerator()
        {
            return this._Projects
                       .Values
                       .Where(p => p.IsHidden == false &&
                                   p.InstallPath != null)
                       .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._Projects
                       .Values
                       .Where(p =>
                              p.IsHidden == false &&
                              p.InstallPath != null)
                       .GetEnumerator();
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
            if (this.ActiveProject == null)
            {
                return HashList<TType>.Dummy;
            }

            return this.ActiveProject.LoadLists(filter, hasher, modifier, extra);
        }
        #endregion

        public string GetSetting(string name, string defaultValue)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (this.ActiveProject == null)
            {
                return defaultValue;
            }

            return this.ActiveProject.GetSetting(name, defaultValue);
        }

        public TType GetSetting<TType>(string name, TType defaultValue)
            where TType : struct
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (this.ActiveProject == null)
            {
                return defaultValue;
            }

            return this.ActiveProject.GetSetting(name, defaultValue);
        }
    }
}
