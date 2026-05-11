using UnityEngine;

/// <summary>
/// Runtime comfort profile applied over every serialized car setup.
/// Keeps individual car differences while making the global handling less twitchy and less icy.
/// </summary>
public static class GlobalCarPhysicsTuning
{
	public const float MotorTorqueMultiplier = 0.82f;
	public const float BrakeTorqueMultiplier = 0.8f;
	public const float SteerAngleChangeSpeedLimit = 120f;
	public const float MinHighSpeedSteerMultiplier = 0.22f;
	public const float HelpSteerPowerLimit = 0.45f;
	public const float OppositeAngularVelocityHelpLimit = 0.09f;
	public const float PositiveAngularVelocityHelpLimit = 0.04f;
	public const float MaxAngularVelocityHelpAngleLimit = 55f;
	public const float MinAngularVelocityAtMaxAngle = 0.9f;
	public const float MaxAngularVelocityAtMinAngle = 1.6f;
	public const float MinForwardFrictionSetting = 0.36f;
	public const float MinSidewaysFrictionSetting = 0.32f;
	public const float MinLinearDamping = 0.12f;
	public const float MinAngularDamping = 0.18f;
	public const float AccelerationInputChangeSpeed = 3.5f;

	public static float TuneMotorTorque (float value)
	{
		return Mathf.Max (0, value * MotorTorqueMultiplier);
	}

	public static float TuneBrakeTorque (float value)
	{
		return Mathf.Max (0, value * BrakeTorqueMultiplier);
	}

	public static float TuneMaxSteerAngle (float value)
	{
		return value;
	}

	public static float TuneSteerAngleChangeSpeed (float value)
	{
		return Mathf.Min (value, SteerAngleChangeSpeedLimit);
	}

	public static float TuneMinSteerAngleMultiplier (float value)
	{
		return Mathf.Clamp01 (Mathf.Max (value, MinHighSpeedSteerMultiplier));
	}

	public static float TuneHelpSteerPower (float value)
	{
		return Mathf.Clamp (value, 0, HelpSteerPowerLimit);
	}

	public static float TuneOppositeAngularVelocityHelpPower (float value)
	{
		return Mathf.Clamp (value, 0, OppositeAngularVelocityHelpLimit);
	}

	public static float TunePositiveAngularVelocityHelpPower (float value)
	{
		return Mathf.Clamp (value, 0, PositiveAngularVelocityHelpLimit);
	}

	public static float TuneMaxAngularVelocityHelpAngle (float value)
	{
		return Mathf.Min (Mathf.Max (value, 1f), MaxAngularVelocityHelpAngleLimit);
	}

	public static float TuneAngularVelucityInMaxAngle (float value)
	{
		return Mathf.Max (value, MinAngularVelocityAtMaxAngle);
	}

	public static float TuneAngularVelucityInMinAngle (float value)
	{
		return Mathf.Min (value, MaxAngularVelocityAtMinAngle);
	}

	public static float TuneForwardFrictionSetting (float value)
	{
		return Mathf.Clamp01 (Mathf.Max (value, MinForwardFrictionSetting));
	}

	public static float TuneSidewaysFrictionSetting (float value)
	{
		return Mathf.Clamp01 (Mathf.Max (value, MinSidewaysFrictionSetting));
	}

	public static float TuneLinearDamping (float value)
	{
		return Mathf.Max (value, MinLinearDamping);
	}

	public static float TuneAngularDamping (float value)
	{
		return Mathf.Max (value, MinAngularDamping);
	}

	public static void ApplyRigidbodyDamping (Rigidbody rb)
	{
		if (rb == null)
			return;

		rb.linearDamping = TuneLinearDamping (rb.linearDamping);
		rb.angularDamping = TuneAngularDamping (rb.angularDamping);
	}
}
