﻿/* Copyright (c) 2019 Rick (rick 'at' gibbed 'dot' us)
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

namespace Gibbed.ProjectData
{
    public sealed class HashList<TType>
    {
        internal Dictionary<TType, string> Lookup;
        internal static HashList<TType> Dummy = new HashList<TType>();

        internal HashList()
        {
            this.Lookup = new Dictionary<TType, string>();
        }

        public void Add(TType index, string value)
        {
            this.Lookup.Add(index, value);
        }

        public bool Contains(TType index)
        {
            return this.Lookup.ContainsKey(index);
        }

        public string this[TType index]
        {
            get
            {
                string value;
                if (this.Lookup.TryGetValue(index, out value) == false)
                {
                    return null;
                }
                return value;
            }
        }

        public IEnumerable<string> GetStrings()
        {
            return this.Lookup.Values;
        }
    }
}
