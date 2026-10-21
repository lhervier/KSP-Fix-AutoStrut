using HarmonyLib;
using UnityEngine;

namespace com.github.lhervier.ksp.fix.autostrut
{
    /// <summary>
    /// Gives every part joint KSP sets up, attach joints and autostruts alike, a twist range of [−177°, +177°]
    /// instead of the [+177°, +177°] stock leaves: the TwistLimit mode.
    /// </summary>
    internal static class JointTwistLimit
    {
        /// <summary>Patches PartJoint.SetJointLimits with <see cref="Postfix"/>.</summary>
        public static void Install(Harmony harmony)
        {
            var target = AccessTools.Method(typeof(PartJoint), "SetJointLimits",
                new[] { typeof(ConfigurableJoint), typeof(bool) });
            harmony.Patch(target, postfix: new HarmonyMethod(typeof(JointTwistLimit), nameof(Postfix)));
        }

        /// <summary>
        /// Sets the lower twist limit of <paramref name="newJoint"/> to the negative of its upper one, unless the
        /// joint is a rigid attachment, whose motions are all locked anyway.
        /// </summary>
        private static void Postfix(ConfigurableJoint newJoint, bool rigidAttach)
        {
            if (rigidAttach || newJoint == null)
            {
                return;
            }
            // SetJointLimits gives the same SoftJointLimit to the upper and the lower twist limit.
            SoftJointLimit low = newJoint.lowAngularXLimit;
            low.limit = -Mathf.Abs(newJoint.highAngularXLimit.limit);
            newJoint.lowAngularXLimit = low;
        }
    }
}
