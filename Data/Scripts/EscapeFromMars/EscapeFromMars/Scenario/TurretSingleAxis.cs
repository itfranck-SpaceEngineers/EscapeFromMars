﻿using System;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.ModAPI;
using VRageMath;
using Duckroll;

namespace EscapeFromMars
{
	internal class TurretSingleAxis : ITurret
	{
		private readonly IMyRemoteControl remoteControl;
		private readonly IMyCubeGrid rotorMountedGrid;
		private readonly IMyMotorStator rotor;
//		private readonly Vector3D position; // Turrets like this don't move
		private bool arePlayersNear;

		internal TurretSingleAxis(IMyRemoteControl remoteControl, IMyCubeGrid rotorMountedGrid, IMyMotorStator rotor)
		{
			this.remoteControl = remoteControl;
			this.rotorMountedGrid = rotorMountedGrid;
			this.rotor = rotor;
//			position = rotorMountedGrid.GetPosition();
		}

		public void Update60()
		{
//            var player = DuckUtils.GetNearestPlayerToPosition(position, 500);
            // change in EFM V 10: allow moveable custom turrets
            var player = DuckUtils.GetNearestPlayerToPosition(rotorMountedGrid.GetPosition(), 300);
            arePlayersNear = player != null;

            // NOTE: gun is shot by sensor on gun turret..
            // so max range of gun is only 50m since that's max range of sensor...
		}

		public void Update1()
		{
			if (!arePlayersNear)
			{
				return;
			}

			if (!remoteControl.IsControlledByFaction("GCORP"))
			{
				return; // No point bothering to remove from the list, it will disappear next time the game reloads
			}

//            var player = DuckUtils.GetNearestPlayerToPosition(position, 300);
            var player = DuckUtils.GetNearestPlayerToPosition(rotorMountedGrid.GetPosition(), 100);
            if (player == null)
			{
				return;
			}

			if (rotorMountedGrid.HasUsableGun())
			{
				TurnToFacePosition(player.GetPosition());
			}
			else
			{
				rotorMountedGrid.AttemptSelfDestruct();
			}
		}

		/// <summary>
		/// This code is taken from Whip's AI Rotor Turret Control Script. All credit to Whiplash.
		/// https://steamcommunity.com/sharedfiles/filedetails/?id=672678005
		/// </summary>
		/// <param name="targetPosition">point to face</param>
		private void TurnToFacePosition(Vector3D targetPosition)
		{
			IMyTerminalBlock turretReference = remoteControl;
			Vector3D turretFrontVec = turretReference.WorldMatrix.Forward;
			Vector3D absUpVec = rotor.WorldMatrix.Up;
			Vector3D turretSideVec = turretReference.WorldMatrix.Right;
			Vector3D turretFrontCrossSide = turretFrontVec.Cross(turretSideVec);
			Vector3D turretLeftVec;
			if (DotIsSameDirection(absUpVec, turretFrontCrossSide))
			{
				turretLeftVec = turretSideVec;
			}
			else
			{
				turretLeftVec = -1 * turretSideVec;
			}
			Vector3D referenceToTargetVec = targetPosition - turretReference.GetPosition();
			//get projections onto axis made out of our plane orientation
			Vector3D projOnFront = VectorProjection(referenceToTargetVec, turretFrontVec);
			Vector3D projOnLeft = VectorProjection(referenceToTargetVec, turretLeftVec);
			Vector3D projOnFrontLeftPlane = projOnFront + projOnLeft;
			double azimuthAngle = Math.Asin(MathHelper.Clamp(projOnLeft.Length() * DotGetSign(projOnLeft, turretLeftVec) / projOnFrontLeftPlane.Length(), -1, 1));
			double azimuthSpeed = 40 * azimuthAngle; //derivitave term is useless as rotors dampen by default

            rotor.TargetVelocityRPM= -(float)azimuthSpeed; //negative because we want to cancel the positive angle via our movements
//            rotor.SetValue("Velocity", -(float) azimuthSpeed); //negative because we want to cancel the positive angle via our movements
		}

		private bool DotIsSameDirection( Vector3D a, Vector3D b )
		{
			var x = a.Dot( b );
			return Math.Abs(Math.Abs(x) - x) < 0.00000000001; // This used to be an equals. Not sure about the tolerance value though.
		}

		private Vector3D VectorProjection( Vector3D a, Vector3D b )
		{
			var projection = a.Dot( b ) / b.Length() / b.Length() * b;
			return projection;
		}

		private double DotGetSign( Vector3D a, Vector3D b )
		{
			var x = a.Dot( b );
			return x/Math.Abs(x);
		}

	}
}