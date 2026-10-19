using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace com.github.lhervier.ksp.autostrutfix
{
    /// <summary>
    /// Once the main menu is reached (KSP Community Fixes has applied its patches by then, and no flight has
    /// started yet), puts the AutoStrutDrift patch of KSP Community Fixes where <see cref="Settings.Mode"/>
    /// needs it: removed in OriginalPose mode, which replaces it; left on in the two twist limit modes, which
    /// rely on it. Says in KSP.log what it found and did.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class KspcfCheck : MonoBehaviour
    {
        private const string KspcfHarmonyId = "KSPCommunityFixes";
        private const string HarmonyId = "com.github.lhervier.ksp.autostrutfix";

        private void Start()
        {
            var target = AccessTools.Method(typeof(Part), "SecureAutoStrut",
                new[] { typeof(Part), typeof(AttachNode), typeof(AttachNode), typeof(bool) });
            Patches patches = target == null ? null : Harmony.GetPatchInfo(target);
            Patch kspcfPostfix = patches?.Postfixes.FirstOrDefault(p => p.owner == KspcfHarmonyId);

            if (Settings.Mode == FixMode.TwistLimitKSPCF || Settings.Mode == FixMode.TwistLimit)
            {
                if (kspcfPostfix == null)
                {
                    Log.Warning($"Mode {Settings.Mode}, but AutoStrutDrift of KSP Community Fixes is not on: the " +
                        "twist limit is fixed, and nothing turns rebuilt autostruts back to the vessel's original shape");
                }
                else
                {
                    Log.Info($"Mode {Settings.Mode}: AutoStrutDrift of KSP Community Fixes left on");
                }
                return;
            }

            if (kspcfPostfix == null)
            {
                Log.Info("Mode OriginalPose: AutoStrutDrift of KSP Community Fixes is not on");
                return;
            }
            // This mod's own postfix sets up again everything AutoStrutDrift sets up: it has nothing left to do.
            new Harmony(HarmonyId).Unpatch(target, kspcfPostfix.PatchMethod);
            Log.Info("Mode OriginalPose: AutoStrutDrift of KSP Community Fixes turned off");
        }
    }
}
