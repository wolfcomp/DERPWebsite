﻿/* Copyright (c) FFXIVQuickLauncher https://github.com/goatcorp/FFXIVQuickLauncher/blob/master/LICENSE
 *
 * Modified to fit the needs of the project.
 */

using DERPWebsite.Patching.ZiPatch.Util;

namespace DERPWebsite.Patching.ZiPatch
{
    public class ZiPatchConfig
    {
        public enum PlatformId : ushort
        {
            Win32 = 0,
            Ps3 = 1,
            Ps4 = 2,
            Unknown = 3
        }

        public string GamePath { get; protected set; }
        public PlatformId Platform { get; set; }
        public bool IgnoreMissing { get; set; }
        public bool IgnoreOldMismatch { get; set; }
        public SqexFileStreamStore Store { get; set; }


        public ZiPatchConfig(string gamePath)
        {
            GamePath = gamePath;
        }
    }
}