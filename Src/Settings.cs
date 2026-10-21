using System;
using System.IO;

namespace com.github.lhervier.ksp.fix.autostrut
{
    /// <summary>How the mod makes a rebuilt autostrut hold the vessel's original shape.</summary>
    internal enum FixMode
    {
        /// <summary>
        /// The autostrut is set up on the original pose; the stock joint is left as it is. AutoStrutDrift of
        /// KSP Community Fixes, which this replaces, is turned off.
        /// </summary>
        OriginalPose,

        /// <summary>
        /// The lower twist limit of the autostrut is set to its negative, so that AutoStrutDrift of KSP
        /// Community Fixes, left on, can turn the joint back to the original pose. The twist of every autostrut
        /// is then held by its spring, as its bending is, instead of by the limit.
        /// </summary>
        TwistLimitKSPCF,

        /// <summary>
        /// As <see cref="TwistLimitKSPCF"/>, but for every part joint KSP sets up (PartJoint.SetJointLimits): attach
        /// joints and every autostrut, those AutoStrutDrift leaves alone included. Every joint of every vessel
        /// then twists as easily as it bends.
        /// </summary>
        TwistLimit
    }

    /// <summary>
    /// What the player can set in PluginData/settings.cfg, next to the DLL. Read once, when KSP starts.
    /// </summary>
    internal static class Settings
    {
        /// <summary>How rebuilt autostruts are fixed.</summary>
        public static FixMode Mode { get; private set; } = FixMode.OriginalPose;

        /// <summary>
        /// Reads the settings file and applies its log level. A missing file, a missing value or an unknown
        /// value leaves the default: the original pose, logging at Info.
        /// </summary>
        public static void Load()
        {
            string folder = Path.GetDirectoryName(typeof(Settings).Assembly.Location);
            string path = Path.Combine(Path.Combine(folder, "PluginData"), "settings.cfg");
            if (!File.Exists(path))
            {
                Log.Warning($"No settings file at {path}, using the defaults");
                return;
            }
            ConfigNode node = ConfigNode.Load(path);
            if (node == null)
            {
                Log.Warning($"Could not read {path}, using the defaults");
                return;
            }

            string level = node.GetValue("logLevel");
            LogLevel parsedLevel;
            if (Enum.TryParse(level, true, out parsedLevel) && Enum.IsDefined(typeof(LogLevel), parsedLevel))
            {
                Log.Level = parsedLevel;
            }
            else
            {
                Log.Warning($"Unknown logLevel '{level}' in {path}, logging at {Log.Level}");
            }

            string mode = node.GetValue("mode");
            FixMode parsedMode;
            if (mode == null)
            {
                return;
            }
            if (Enum.TryParse(mode, true, out parsedMode) && Enum.IsDefined(typeof(FixMode), parsedMode))
            {
                Mode = parsedMode;
            }
            else
            {
                Log.Warning($"Unknown mode '{mode}' in {path}, using {Mode}");
            }
        }
    }
}
