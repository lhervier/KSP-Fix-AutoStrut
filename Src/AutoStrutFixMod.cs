using System;
using HarmonyLib;
using UnityEngine;

namespace com.github.lhervier.ksp.autostrutfix
{
    /// <summary>
    /// Installs the fix once, when KSP starts: from then on, every autostrut KSP builds in flight holds the
    /// vessel's original shape, even when it is built while the vessel is bent.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class AutoStrutFixMod : MonoBehaviour
    {
        private const string HarmonyId = "com.github.lhervier.ksp.autostrutfix";

        private void Start()
        {
            Settings.Load();
            Log.Info($"Version {typeof(AutoStrutFixMod).Assembly.GetName().Version}, mode {Settings.Mode}, " +
                $"log level {Log.Level}");
            Harmony harmony = new Harmony(HarmonyId);
            try
            {
                AutoStrutPose.Install(harmony);
                Log.Info("Autostrut fix installed");
            }
            catch (Exception e)
            {
                // Whatever patch did get applied does nothing on its own: the autostruts are exactly stock.
                Log.Error($"Could not install the autostrut fix, autostruts are left as stock builds them: {e}");
            }
            if (Settings.Mode == FixMode.TwistLimit)
            {
                try
                {
                    JointTwistLimit.Install(harmony);
                    Log.Info("Twist limit of every part joint fixed");
                }
                catch (Exception e)
                {
                    Log.Error($"Could not fix the twist limit of part joints, they are left as stock sets them: {e}");
                }
            }
        }
    }
}
