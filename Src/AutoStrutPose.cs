using HarmonyLib;
using UnityEngine;

namespace com.github.lhervier.ksp.fix.autostrut
{
    /// <summary>
    /// Makes every autostrut built in flight hold its part where the vessel's original layout puts it relative
    /// to the strut's anchor, rather than where the part happens to be at that instant, in the way
    /// <see cref="Settings.Mode"/> says.
    ///
    /// KSP rebuilds the autostruts of a vessel on the physics step after any change to it: a part breaking
    /// off, a decoupling, a docking, the vessel going off rails. A joint made while the vessel is bent (a part
    /// lost in the middle of a rollover, say) holds that bent pose for good, against the part's attach joint
    /// that holds the original one: the solver never settles, the rigidbodies report speeds that do not move
    /// them, and a rover's tyres turn those into a spin on the spot, brakes on.
    /// </summary>
    internal static class AutoStrutPose
    {
        /// <summary>Patches Part.SecureAutoStrut with <see cref="Postfix"/>.</summary>
        public static void Install(Harmony harmony)
        {
            var target = AccessTools.Method(typeof(Part), "SecureAutoStrut",
                new[] { typeof(Part), typeof(AttachNode), typeof(AttachNode), typeof(bool) });
            // Low priority: after KSPCommunityFixes' AutoStrutDrift postfix, whose setup this one replaces in
            // OriginalPose mode if it is still there, and completes in TwistLimitKSPCF mode.
            harmony.Patch(target, postfix: new HarmonyMethod(typeof(AutoStrutPose), nameof(Postfix))
            {
                priority = Priority.Low
            });
        }

        /// <summary>
        /// Makes the autostrut <paramref name="__result"/>, just built from <paramref name="__instance"/> to
        /// <paramref name="anchor"/>, able to rest at the pose the original layout (orgPos, orgRot) gives the
        /// part relative to the anchor. In OriginalPose mode, the joint is set up on that pose; in TwistLimitKSPCF
        /// mode, its lower twist limit is fixed, for AutoStrutDrift of KSP Community Fixes to do the rest. Leaves
        /// the part where it is. Does nothing outside flight, or for a strut not joining the two parts' own
        /// rigidbodies (a robotic part in between).
        /// </summary>
        private static void Postfix(Part __instance, Part anchor, PartJoint __result)
        {
            if (!HighLogic.LoadedSceneIsFlight || __result == null || __result.Joint == null || anchor == null)
            {
                return;
            }
            Rigidbody rb = __instance.rb;
            ConfigurableJoint joint = __result.Joint;
            if (rb == null || anchor.rb == null || joint.connectedBody != anchor.rb || joint.GetComponent<Rigidbody>() != rb)
            {
                return;
            }

            // The part's pose relative to the anchor in the original layout, and where that puts it now.
            Quaternion originalRelRot = Quaternion.Inverse(anchor.orgRot) * __instance.orgRot;
            Vector3 originalRelPos = Quaternion.Inverse(anchor.orgRot) * (__instance.orgPos - anchor.orgPos);
            Transform anchorTransform = anchor.transform;
            Vector3 originalPos = anchorTransform.position + anchorTransform.rotation * originalRelPos;
            Quaternion originalRot = anchorTransform.rotation * originalRelRot;

            Transform t = __instance.transform;
            Vector3 pos = t.position;
            Quaternion rot = t.rotation;

            if (Settings.Mode == FixMode.TwistLimitKSPCF || Settings.Mode == FixMode.TwistLimit)
            {
                // In TwistLimit mode, JointTwistLimit has already done it, for every joint; doing it
                // again here only keeps the log line.
                // PartJoint.SetJointLimits gives the twist +180 as its lower limit too, which leaves the twist
                // range at [+177, +177] and holds the twist where the joint was set up. With its negative,
                // targetRotation (set by AutoStrutDrift) can turn the twist as well.
                SoftJointLimit low = joint.lowAngularXLimit;
                low.limit = -Mathf.Abs(joint.highAngularXLimit.limit);
                joint.lowAngularXLimit = low;
                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"Autostrut from {__instance.partInfo.name} to {anchor.partInfo.name} given a twist " +
                        $"range of [{joint.lowAngularXLimit.limit}, {joint.highAngularXLimit.limit}]; the part was " +
                        $"{Quaternion.Angle(rot, originalRot):F3}° and {(pos - originalPos).magnitude:F4} m away " +
                        "from the original layout");
                }
                return;
            }

            // A ConfigurableJoint rests at the relative pose its bodies have when its connected body and axes are
            // assigned. Rotating the target instead (targetRotation, as KSPCF does) cannot reach the twist:
            // PartJoint.SetJointLimits leaves the twist range at [+177, +177], which holds the twist where it was
            // captured. So the part is put at its original pose for that instant, then put back where it was.
            MoveTo(t, rb, originalPos, originalRot);

            Vector3 axis = joint.axis;
            Vector3 secondaryAxis = joint.secondaryAxis;
            joint.connectedBody = null;
            joint.connectedBody = anchor.rb;
            joint.axis = axis;
            joint.secondaryAxis = secondaryAxis;
            joint.anchor = Vector3.zero;
            joint.connectedAnchor = originalRelPos;
            joint.targetRotation = Quaternion.identity;
            joint.targetPosition = Vector3.zero;

            MoveTo(t, rb, pos, rot);

            if (Log.IsDebugEnabled)
            {
                Log.Debug($"Autostrut from {__instance.partInfo.name} to {anchor.partInfo.name} set on the original " +
                    $"layout; the part was {Quaternion.Angle(rot, originalRot):F3}° and " +
                    $"{(pos - originalPos).magnitude:F4} m away from it");
            }
        }

        // Puts a part's transform and rigidbody at a pose, and lets the physics engine know at once.
        private static void MoveTo(Transform t, Rigidbody rb, Vector3 position, Quaternion rotation)
        {
            t.SetPositionAndRotation(position, rotation);
            rb.position = position;
            rb.rotation = rotation;
            Physics.SyncTransforms();
        }
    }
}
