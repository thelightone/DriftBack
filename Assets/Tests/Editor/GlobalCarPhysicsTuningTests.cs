using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class GlobalCarPhysicsTuningTests
{
	[Test]
	public void ComfortProfileReducesOverpoweredAndInstantInputs ()
	{
		Assert.That (GlobalCarPhysicsTuning.TuneMotorTorque (800f), Is.EqualTo (656f).Within (0.001f));
		Assert.That (GlobalCarPhysicsTuning.TuneMaxSteerAngle (42f), Is.EqualTo (34f).Within (0.001f));
		Assert.That (GlobalCarPhysicsTuning.TuneSteerAngleChangeSpeed (240f), Is.EqualTo (120f).Within (0.001f));
		Assert.That (GlobalCarPhysicsTuning.TuneSteerAngleChangeSpeed (60f), Is.EqualTo (60f).Within (0.001f));
	}

	[Test]
	public void ComfortProfileLimitsDriftHelpersWithoutReversingThem ()
	{
		Assert.That (GlobalCarPhysicsTuning.TuneHelpSteerPower (1f), Is.EqualTo (0.45f).Within (0.001f));
		Assert.That (GlobalCarPhysicsTuning.TuneOppositeAngularVelocityHelpPower (0.2f), Is.EqualTo (0.09f).Within (0.001f));
		Assert.That (GlobalCarPhysicsTuning.TunePositiveAngularVelocityHelpPower (-0.1f), Is.EqualTo (0f).Within (0.001f));
		Assert.That (GlobalCarPhysicsTuning.TuneMaxAngularVelocityHelpAngle (90f), Is.EqualTo (55f).Within (0.001f));
	}

	[Test]
	public void ComfortProfileAddsBaselineGripAndDamping ()
	{
		Assert.That (GlobalCarPhysicsTuning.TuneForwardFrictionSetting (0.3f), Is.EqualTo (0.36f).Within (0.001f));
		Assert.That (GlobalCarPhysicsTuning.TuneForwardFrictionSetting (0.5f), Is.EqualTo (0.5f).Within (0.001f));
		Assert.That (GlobalCarPhysicsTuning.TuneSidewaysFrictionSetting (0f), Is.EqualTo (0.32f).Within (0.001f));
		Assert.That (GlobalCarPhysicsTuning.TuneSidewaysFrictionSetting (0.6f), Is.EqualTo (0.6f).Within (0.001f));
		Assert.That (GlobalCarPhysicsTuning.TuneLinearDamping (0.05f), Is.EqualTo (0.12f).Within (0.001f));
		Assert.That (GlobalCarPhysicsTuning.TuneAngularDamping (0.05f), Is.EqualTo (0.18f).Within (0.001f));
	}
}

public static class GlobalCarPhysicsTuningSmokeRunner
{
	public static void Run ()
	{
		try
		{
			AssertApproximately ("motor torque", GlobalCarPhysicsTuning.TuneMotorTorque (800f), 656f);
			AssertApproximately ("max steer angle", GlobalCarPhysicsTuning.TuneMaxSteerAngle (42f), 34f);
			AssertApproximately ("steer speed", GlobalCarPhysicsTuning.TuneSteerAngleChangeSpeed (240f), 120f);
			AssertApproximately ("help steer", GlobalCarPhysicsTuning.TuneHelpSteerPower (1f), 0.45f);
			AssertApproximately ("negative angular help", GlobalCarPhysicsTuning.TunePositiveAngularVelocityHelpPower (-0.1f), 0f);
			AssertApproximately ("forward friction", GlobalCarPhysicsTuning.TuneForwardFrictionSetting (0.3f), 0.36f);
			AssertApproximately ("sideways friction", GlobalCarPhysicsTuning.TuneSidewaysFrictionSetting (0f), 0.32f);
			AssertApproximately ("linear damping", GlobalCarPhysicsTuning.TuneLinearDamping (0.05f), 0.12f);
			AssertApproximately ("angular damping", GlobalCarPhysicsTuning.TuneAngularDamping (0.05f), 0.18f);

			Debug.Log ("GlobalCarPhysicsTuning smoke checks passed.");
			EditorApplication.Exit (0);
		}
		catch (System.Exception ex)
		{
			Debug.LogException (ex);
			EditorApplication.Exit (1);
		}
	}

	static void AssertApproximately (string name, float actual, float expected)
	{
		if (!Mathf.Approximately (actual, expected))
			throw new System.Exception ($"{name}: expected {expected}, got {actual}");
	}
}
