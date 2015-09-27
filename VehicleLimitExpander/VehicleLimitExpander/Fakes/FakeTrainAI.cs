using ColossalFramework;
using ColossalFramework.Math;
using System;
using UnityEngine;

namespace Klyte.Unlimiter.Fake
{
	public class FakeTrainAI:TrainAI
	{
		private static ushort CheckOverlap (ushort vehicleID, ref Vehicle vehicleData, Segment3 segment, ushort ignoreVehicle, ushort otherID, ref Vehicle otherData, ref bool overlap, Vector3 min, Vector3 max)
		{
			if (ignoreVehicle == 0 || (otherID != ignoreVehicle && otherData.m_leadingVehicle != ignoreVehicle && otherData.m_trailingVehicle != ignoreVehicle))
			{
				VehicleInfo info = otherData.Info;
				if (info.m_vehicleType == VehicleInfo.VehicleType.Bicycle)
				{
					return otherData.m_nextGridVehicle;
				}
				if (((vehicleData.m_flags | otherData.m_flags) & Vehicle.Flags.Transition) == Vehicle.Flags.None && (vehicleData.m_flags & Vehicle.Flags.Underground) != (otherData.m_flags & Vehicle.Flags.Underground))
				{
					return otherData.m_nextGridVehicle;
				}
				Vector3 vector = Vector3.Min (otherData.m_segment.Min (), otherData.m_targetPos3);
				Vector3 vector2 = Vector3.Max (otherData.m_segment.Max (), otherData.m_targetPos3);
				if (min.x < vector2.x + 2f && min.y < vector2.y + 2f && min.z < vector2.z + 2f && vector.x < max.x + 2f && vector.y < max.y + 2f && vector.z < max.z + 2f)
				{
					Vector3 rhs = Vector3.Normalize (segment.b - segment.a);
					Vector3 lhs = otherData.m_segment.a - vehicleData.m_segment.b;
					Vector3 lhs2 = otherData.m_segment.b - vehicleData.m_segment.b;
					if (Vector3.Dot (lhs, rhs) >= 1f || Vector3.Dot (lhs2, rhs) >= 1f)
					{
						float num2;
						float num3;
						float num = segment.DistanceSqr (otherData.m_segment, out num2, out num3);
						if (num < 4f)
						{
							overlap = true;
						}
						Vector3 a = otherData.m_segment.b;
						segment.a.y = segment.a.y * 0.5f;
						segment.b.y = segment.b.y * 0.5f;
						for (int i = 0; i < 4; i++)
						{
							Vector3 vector3 = otherData.GetTargetPos (i);
							Segment3 segment2 = new Segment3 (a, vector3);
							segment2.a.y = segment2.a.y * 0.5f;
							segment2.b.y = segment2.b.y * 0.5f;
							if (segment2.LengthSqr () > 0.01f)
							{
								num = segment.DistanceSqr (segment2, out num2, out num3);
								if (num < 4f)
								{
									overlap = true;
									break;
								}
							}
							a = vector3;
						}
					}
				}
			}
			return otherData.m_nextGridVehicle;
		}
		
		private static bool CheckOverlap (ushort vehicleID, ref Vehicle vehicleData, Segment3 segment, ushort ignoreVehicle)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			Vector3 min = segment.Min ();
			Vector3 max = segment.Max ();
			int num = Mathf.Max ((int)((min.x - 30f) / 32f + 270f), 0);
			int num2 = Mathf.Max ((int)((min.z - 30f) / 32f + 270f), 0);
			int num3 = Mathf.Min ((int)((max.x + 30f) / 32f + 270f), 539);
			int num4 = Mathf.Min ((int)((max.z + 30f) / 32f + 270f), 539);
			bool result = false;
			for (int i = num2; i <= num4; i++)
			{
				for (int j = num; j <= num3; j++)
				{
					ushort num5 = instance.m_vehicleGrid [i * 540 + j];
					int num6 = 0;
					while (num5 != 0)
					{
						num5 = CheckOverlap (vehicleID, ref vehicleData, segment, ignoreVehicle, num5, ref instance.m_vehicles.m_buffer [(int)num5], ref result, min, max);
						if (++num6 > 65536)
						{
							CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
							break;
						}
					}
				}
			}
			return result;
		}

		private static float GetMaxSpeed (ushort leaderID, ref Vehicle leaderData)
		{
			float num = 1000000f;
			VehicleManager instance = Singleton<VehicleManager>.instance;
			ushort num2 = leaderID;
			int num3 = 0;
			while (num2 != 0)
			{
				num = Mathf.Min (num, instance.m_vehicles.m_buffer [(int)num2].m_targetPos0.w);
				num = Mathf.Min (num, instance.m_vehicles.m_buffer [(int)num2].m_targetPos1.w);
				num2 = instance.m_vehicles.m_buffer [(int)num2].m_trailingVehicle;
				if (++num3 > 65536)
				{
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			return num;
		}

		private static void ResetTargets (ushort vehicleID, ref Vehicle vehicleData, ushort leaderID, ref Vehicle leaderData, bool pushPathPos)
		{
			Vehicle.Frame lastFrameData = vehicleData.GetLastFrameData ();
			VehicleInfo info = vehicleData.Info;
			TrainAI trainAI = info.m_vehicleAI as TrainAI;
			Vector3 vector = lastFrameData.m_position;
			Vector3 vector2 = lastFrameData.m_position;
			Vector3 b = lastFrameData.m_rotation * new Vector3 (0f, 0f, info.m_generatedInfo.m_wheelBase * 0.5f);
			if ((leaderData.m_flags & Vehicle.Flags.Reversed) != Vehicle.Flags.None)
			{
				vector -= b;
				vector2 += b;
			}
			else
			{
				vector += b;
				vector2 -= b;
			}
			vehicleData.m_targetPos0 = vector2;
			vehicleData.m_targetPos0.w = 2f;
			vehicleData.m_targetPos1 = vector;
			vehicleData.m_targetPos1.w = 2f;
			vehicleData.m_targetPos2 = vehicleData.m_targetPos1;
			vehicleData.m_targetPos3 = vehicleData.m_targetPos1;
			if (vehicleData.m_path != 0u)
			{
				PathManager instance = Singleton<PathManager>.instance;
				int num = (vehicleData.m_pathPositionIndex >> 1) + 1;
				uint num2 = vehicleData.m_path;
				if (num >= (int)instance.m_pathUnits.m_buffer [(int)((UIntPtr)num2)].m_positionCount)
				{
					num = 0;
					num2 = instance.m_pathUnits.m_buffer [(int)((UIntPtr)num2)].m_nextPathUnit;
				}
				PathUnit.Position pathPos;
				if (instance.m_pathUnits.m_buffer [(int)((UIntPtr)vehicleData.m_path)].GetPosition (vehicleData.m_pathPositionIndex >> 1, out pathPos))
				{
					uint laneID = PathManager.GetLaneID (pathPos);
					PathUnit.Position pathPos2;
					if (num2 != 0u && instance.m_pathUnits.m_buffer [(int)((UIntPtr)num2)].GetPosition (num, out pathPos2))
					{
						uint laneID2 = PathManager.GetLaneID (pathPos2);
						if (laneID2 == laneID)
						{
							if (num2 != vehicleData.m_path)
							{
								instance.ReleaseFirstUnit (ref vehicleData.m_path);
							}
							vehicleData.m_pathPositionIndex = (byte)(num << 1);
						}
					}
					PathUnit.CalculatePathPositionOffset (laneID, vector2, out vehicleData.m_lastPathOffset);
				}
			}
			if (vehicleData.m_path != 0u)
			{
				int num3 = 0;
				((FakeTrainAI)trainAI).UpdatePathTargetPositions (vehicleID, ref vehicleData, vector2, vector, 0, ref leaderData, ref num3, 1, 4, 4f, 1f);
			}
		}

		private static void InitializePath (ushort vehicleID, ref Vehicle vehicleData)
		{
			PathManager instance = Singleton<PathManager>.instance;
			VehicleManager instance2 = Singleton<VehicleManager>.instance;
			ushort trailingVehicle = vehicleData.m_trailingVehicle;
			int num = 0;
			while (trailingVehicle != 0)
			{
				if (instance2.m_vehicles.m_buffer [(int)trailingVehicle].m_path != 0u)
				{
					instance.ReleasePath (instance2.m_vehicles.m_buffer [(int)trailingVehicle].m_path);
					instance2.m_vehicles.m_buffer [(int)trailingVehicle].m_path = 0u;
				}
				if (instance.AddPathReference (vehicleData.m_path))
				{
					instance2.m_vehicles.m_buffer [(int)trailingVehicle].m_path = vehicleData.m_path;
					instance2.m_vehicles.m_buffer [(int)trailingVehicle].m_pathPositionIndex = 0;
				}
				ResetTargets (trailingVehicle, ref instance2.m_vehicles.m_buffer [(int)trailingVehicle], vehicleID, ref vehicleData, false);
				trailingVehicle = instance2.m_vehicles.m_buffer [(int)trailingVehicle].m_trailingVehicle;
				if (++num > 65536)
				{
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			vehicleData.m_pathPositionIndex = 0;
			ResetTargets (vehicleID, ref vehicleData, vehicleID, ref vehicleData, false);
		}
		private static void Reverse (ushort leaderID, ref Vehicle leaderData)
		{
			if ((leaderData.m_flags & Vehicle.Flags.Reversed) != Vehicle.Flags.None)
			{
				leaderData.m_flags &= ~Vehicle.Flags.Reversed;
			}
			else
			{
				leaderData.m_flags |= Vehicle.Flags.Reversed;
			}
			VehicleManager instance = Singleton<VehicleManager>.instance;
			ushort num = leaderID;
			int num2 = 0;
			while (num != 0)
			{
				ResetTargets (num, ref instance.m_vehicles.m_buffer [(int)num], leaderID, ref leaderData, true);
				instance.m_vehicles.m_buffer [(int)num].m_flags = ((instance.m_vehicles.m_buffer [(int)num].m_flags & ~Vehicle.Flags.Reversed) | (leaderData.m_flags & Vehicle.Flags.Reversed));
				num = instance.m_vehicles.m_buffer [(int)num].m_trailingVehicle;
				if (++num2 > 65536)
				{
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
		}

		public override void LoadVehicle (ushort vehicleID, ref Vehicle data)
		{
			base.LoadVehicle (vehicleID, ref data);
			if (data.m_leadingVehicle == 0 && data.m_trailingVehicle != 0 && (data.m_flags & Vehicle.Flags.Reversed) != Vehicle.Flags.None) {
				VehicleManager instance = Singleton<VehicleManager>.instance;
				ushort num = data.m_trailingVehicle;
				int num2 = 0;
				while (num != 0) {
					ushort trailingVehicle = instance.m_vehicles.m_buffer [(int)num].m_trailingVehicle;
					Vehicle[] expr_6B_cp_0 = instance.m_vehicles.m_buffer;
					ushort expr_6B_cp_1 = num;
					expr_6B_cp_0 [(int)expr_6B_cp_1].m_flags = (expr_6B_cp_0 [(int)expr_6B_cp_1].m_flags | Vehicle.Flags.Reversed);
					num = trailingVehicle;
					if (++num2 > 65536) {
						CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
						break;
					}
				}
			}
		}
		protected virtual bool PathFindReady (ushort vehicleID, ref Vehicle vehicleData)
		{
			PathManager instance = Singleton<PathManager>.instance;
			NetManager instance2 = Singleton<NetManager>.instance;
			float num = vehicleData.CalculateTotalLength (vehicleID);
			float distance = (num + this.m_info.m_generatedInfo.m_wheelBase - this.m_info.m_generatedInfo.m_size.z) * 0.5f;
			Vector3 vector = vehicleData.GetLastFramePosition ();
			PathUnit.Position pathPos;
			if ((vehicleData.m_flags & Vehicle.Flags.Spawned) == Vehicle.Flags.None && instance.m_pathUnits.m_buffer [(int)((UIntPtr)vehicleData.m_path)].GetPosition (0, out pathPos))
			{
				uint laneID = PathManager.GetLaneID (pathPos);
				vector = instance2.m_lanes.m_buffer [(int)((UIntPtr)laneID)].CalculatePosition ((float)pathPos.m_offset * 0.003921569f);
			}
			vehicleData.m_flags &= ~Vehicle.Flags.WaitingPath;
			instance.m_pathUnits.m_buffer [(int)((UIntPtr)vehicleData.m_path)].MoveLastPosition (vehicleData.m_path, distance);
			if ((vehicleData.m_flags & Vehicle.Flags.Spawned) != Vehicle.Flags.None)
			{
				InitializePath (vehicleID, ref vehicleData);
			}
			else
			{
				int index = Mathf.Min (1, (int)(instance.m_pathUnits.m_buffer [(int)((UIntPtr)vehicleData.m_path)].m_positionCount - 1));
				PathUnit.Position pathPos2;
				if (instance.m_pathUnits.m_buffer [(int)((UIntPtr)vehicleData.m_path)].GetPosition (index, out pathPos2))
				{
					uint laneID2 = PathManager.GetLaneID (pathPos2);
					Vector3 a = instance2.m_lanes.m_buffer [(int)((UIntPtr)laneID2)].CalculatePosition ((float)pathPos2.m_offset * 0.003921569f);
					Vector3 forward = a - vector;
					vehicleData.m_frame0.m_position = vector;
					if (forward.sqrMagnitude > 1f)
					{
						float length = instance2.m_lanes.m_buffer [(int)((UIntPtr)laneID2)].m_length;
						vehicleData.m_frame0.m_position = vehicleData.m_frame0.m_position + forward.normalized * Mathf.Min (length * 0.5f, (num - this.m_info.m_generatedInfo.m_size.z) * 0.5f);
						vehicleData.m_frame0.m_rotation = Quaternion.LookRotation (forward);
					}
					vehicleData.m_frame1 = vehicleData.m_frame0;
					vehicleData.m_frame2 = vehicleData.m_frame0;
					vehicleData.m_frame3 = vehicleData.m_frame0;
					this.FrameDataUpdated (vehicleID, ref vehicleData, ref vehicleData.m_frame0);
				}
				this.TrySpawn (vehicleID, ref vehicleData);
			}
			return true;
		}

		public override void SimulationStep (ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
		{
			if ((data.m_flags & Vehicle.Flags.WaitingPath) != Vehicle.Flags.None) {
				byte pathFindFlags = Singleton<PathManager>.instance.m_pathUnits.m_buffer [(int)((UIntPtr)data.m_path)].m_pathFindFlags;
				if ((pathFindFlags & 4) != 0) {
					this.PathFindReady (vehicleID, ref data);
				} else {
					if ((pathFindFlags & 8) != 0 || data.m_path == 0u) {
						data.m_flags &= ~Vehicle.Flags.WaitingPath;
						Singleton<PathManager>.instance.ReleasePath (data.m_path);
						data.m_path = 0u;
						data.Unspawn (vehicleID);
						return;
					}
				}
			} else {
				if ((data.m_flags & Vehicle.Flags.WaitingSpace) != Vehicle.Flags.None) {
					this.TrySpawn (vehicleID, ref data);
				}
			}
			bool flag = (data.m_flags & Vehicle.Flags.Reversed) != Vehicle.Flags.None;
			ushort num;
			if (flag) {
				num = data.GetLastVehicle (vehicleID);
			} else {
				num = vehicleID;
			}
			VehicleManager instance = Singleton<VehicleManager>.instance;
			VehicleInfo info = instance.m_vehicles.m_buffer [(int)num].Info;
			info.m_vehicleAI.SimulationStep (num, ref instance.m_vehicles.m_buffer [(int)num], vehicleID, ref data, 0);
			if ((data.m_flags & (Vehicle.Flags.Created | Vehicle.Flags.Deleted)) != Vehicle.Flags.Created) {
				return;
			}
			bool flag2 = (data.m_flags & Vehicle.Flags.Reversed) != Vehicle.Flags.None;
			if (flag2 != flag) {
				flag = flag2;
				if (flag) {
					num = data.GetLastVehicle (vehicleID);
				} else {
					num = vehicleID;
				}
				info = instance.m_vehicles.m_buffer [(int)num].Info;
				info.m_vehicleAI.SimulationStep (num, ref instance.m_vehicles.m_buffer [(int)num], vehicleID, ref data, 0);
				if ((data.m_flags & (Vehicle.Flags.Created | Vehicle.Flags.Deleted)) != Vehicle.Flags.Created) {
					return;
				}
				flag2 = ((data.m_flags & Vehicle.Flags.Reversed) != Vehicle.Flags.None);
				if (flag2 != flag) {
					Singleton<VehicleManager>.instance.ReleaseVehicle (vehicleID);
					return;
				}
			}
			if (flag) {
				num = instance.m_vehicles.m_buffer [(int)num].m_leadingVehicle;
				int num2 = 0;
				while (num != 0) {
					info = instance.m_vehicles.m_buffer [(int)num].Info;
					info.m_vehicleAI.SimulationStep (num, ref instance.m_vehicles.m_buffer [(int)num], vehicleID, ref data, 0);
					if ((data.m_flags & (Vehicle.Flags.Created | Vehicle.Flags.Deleted)) != Vehicle.Flags.Created) {
						return;
					}
					num = instance.m_vehicles.m_buffer [(int)num].m_leadingVehicle;
					if (++num2 > 65536) {
						CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
						break;
					}
				}
			} else {
				num = instance.m_vehicles.m_buffer [(int)num].m_trailingVehicle;
				int num3 = 0;
				while (num != 0) {
					info = instance.m_vehicles.m_buffer [(int)num].Info;
					info.m_vehicleAI.SimulationStep (num, ref instance.m_vehicles.m_buffer [(int)num], vehicleID, ref data, 0);
					if ((data.m_flags & (Vehicle.Flags.Created | Vehicle.Flags.Deleted)) != Vehicle.Flags.Created) {
						return;
					}
					num = instance.m_vehicles.m_buffer [(int)num].m_trailingVehicle;
					if (++num3 > 65536) {
						CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
						break;
					}
				}
			}
			if ((data.m_flags & (Vehicle.Flags.Spawned | Vehicle.Flags.WaitingPath | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo)) == Vehicle.Flags.None || data.m_blockCounter == 255) {
				Singleton<VehicleManager>.instance.ReleaseVehicle (vehicleID);
			}
		}


	}
}
