using ColossalFramework;
using ColossalFramework.Math;
using System;
using UnityEngine;

namespace Klyte.Unlimiter.Fake
{
	public class FakeShipAI : VehicleAI
	{
		private static float CalculateMaxSpeed (float targetDistance, float targetSpeed, float maxBraking)
		{
			float num = 0.5f * maxBraking;
			float num2 = num + targetSpeed;
			return Mathf.Sqrt (Mathf.Max (0f, num2 * num2 + 2f * targetDistance * maxBraking)) - num;
		}
		

		public override bool CanSpawnAt (Vector3 pos)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			int num = Mathf.Max ((int)((pos.x - 300f) / 320f + 27f), 0);
			int num2 = Mathf.Max ((int)((pos.z - 300f) / 320f + 27f), 0);
			int num3 = Mathf.Min ((int)((pos.x + 300f) / 320f + 27f), 53);
			int num4 = Mathf.Min ((int)((pos.z + 300f) / 320f + 27f), 53);
			for (int i = num2; i <= num4; i++) {
				for (int j = num; j <= num3; j++) {
					ushort num5 = instance.m_vehicleGrid2 [i * 54 + j];
					int num6 = 0;
					while (num5 != 0) {
						if (Vector3.SqrMagnitude (instance.m_vehicles.m_buffer [(int)num5].GetLastFramePosition () - pos) < 90000f) {
							return false;
						}
						num5 = instance.m_vehicles.m_buffer [(int)num5].m_nextGridVehicle;
						if (++num6 > 65536) {
							CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
							break;
						}
					}
				}
			}
			return true;
		}

		public override void SimulationStep (ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
		{
			if ((data.m_flags & Vehicle.Flags.WaitingPath) != Vehicle.Flags.None) {
				PathManager instance = Singleton<PathManager>.instance;
				byte pathFindFlags = instance.m_pathUnits.m_buffer [(int)((UIntPtr)data.m_path)].m_pathFindFlags;
				if ((pathFindFlags & 4) != 0) {
					data.m_pathPositionIndex = 255;
					data.m_flags &= ~Vehicle.Flags.WaitingPath;
					this.TrySpawn (vehicleID, ref data);
				} else {
					if ((pathFindFlags & 8) != 0) {
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
			this.SimulationStep (vehicleID, ref data, vehicleID, ref data, 0);
			if (data.m_leadingVehicle == 0 && data.m_trailingVehicle != 0) {
				VehicleManager instance2 = Singleton<VehicleManager>.instance;
				ushort num = data.m_trailingVehicle;
				int num2 = 0;
				while (num != 0) {
					ushort trailingVehicle = instance2.m_vehicles.m_buffer [(int)num].m_trailingVehicle;
					VehicleInfo info = instance2.m_vehicles.m_buffer [(int)num].Info;
					info.m_vehicleAI.SimulationStep (num, ref instance2.m_vehicles.m_buffer [(int)num], vehicleID, ref data, 0);
					num = trailingVehicle;
					if (++num2 > 65536) {
						CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
						break;
					}
				}
			}
			if ((data.m_flags & (Vehicle.Flags.Spawned | Vehicle.Flags.WaitingPath | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo)) == Vehicle.Flags.None || data.m_blockCounter == 255) {
				Singleton<VehicleManager>.instance.ReleaseVehicle (vehicleID);
			}
		}
		private ushort CheckOtherVehicle (ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ref float maxSpeed, ref bool blocked, float maxBraking, ushort otherID, ref Vehicle otherData, Vector3 min, Vector3 max, int lodPhysics)
		{
			if (otherID != vehicleID && vehicleData.m_leadingVehicle != otherID && vehicleData.m_trailingVehicle != otherID)
			{
				Vector3 vector;
				Vector3 vector2;
				if (lodPhysics >= 1)
				{
					vector = otherData.m_segment.Min ();
					vector2 = otherData.m_segment.Max ();
				}
				else
				{
					vector = Vector3.Min (otherData.m_segment.Min (), otherData.m_targetPos3);
					vector2 = Vector3.Max (otherData.m_segment.Max (), otherData.m_targetPos3);
				}
				if (min.x < vector2.x + 2f && min.y < vector2.y + 2f && min.z < vector2.z + 2f && vector.x < max.x + 2f && vector.y < max.y + 2f && vector.z < max.z + 2f)
				{
					Vehicle.Frame lastFrameData = otherData.GetLastFrameData ();
					VehicleInfo info = otherData.Info;
					float num = frameData.m_velocity.magnitude + 0.01f;
					float num2 = lastFrameData.m_velocity.magnitude;
					float num3 = num2 * (0.5f + 0.5f * num2 / info.m_braking) + info.m_generatedInfo.m_size.z * Mathf.Min (0.5f, num2 * 0.1f);
					num2 += 0.01f;
					float num4 = 0f;
					Vector3 vector3 = frameData.m_position;
					Vector3 lhs = ((Vector3) vehicleData.m_targetPos3) - frameData.m_position;
					for (int i = 1; i < 4; i++)
					{
						Vector3 vector4 = vehicleData.GetTargetPos (i);
						Vector3 vector5 = vector4 - vector3;
						if (Vector3.Dot (lhs, vector5) > 0f)
						{
							float magnitude = vector5.magnitude;
							Segment3 segment = new Segment3 (vector3, vector4);
							min = segment.Min ();
							max = segment.Max ();
							segment.a.y = segment.a.y * 0.5f;
							segment.b.y = segment.b.y * 0.5f;
							if (magnitude > 0.01f && min.x < vector2.x + 2f && min.y < vector2.y + 2f && min.z < vector2.z + 2f && vector.x < max.x + 2f && vector.y < max.y + 2f && vector.z < max.z + 2f)
							{
								Vector3 a = otherData.m_segment.a;
								a.y *= 0.5f;
								float num5;
								if (segment.DistanceSqr (a, out num5) < 400f)
								{
									float num6 = Vector3.Dot (lastFrameData.m_velocity, vector5) / magnitude;
									float num7 = num4 + magnitude * num5;
									if (num7 >= 0.01f)
									{
										num7 -= num6 + 30f;
										float num8 = Mathf.Max (0f, CalculateMaxSpeed (num7, num6, maxBraking));
										if (num8 < 0.01f)
										{
											blocked = true;
										}
										Vector3 rhs = Vector3.Normalize (((Vector3)otherData.m_targetPos3) - otherData.GetLastFramePosition ());
										float num9 = 1.2f - 1f / ((float)vehicleData.m_blockCounter * 0.02f + 0.5f);
										if (Vector3.Dot (vector5, rhs) > num9 * magnitude)
										{
											maxSpeed = Mathf.Min (maxSpeed, num8);
										}
									}
									break;
								}
								if (lodPhysics == 0)
								{
									float num10 = 0f;
									float num11 = num3;
									Vector3 vector6 = otherData.GetLastFramePosition ();
									Vector3 lhs2 = ((Vector3)otherData.m_targetPos3) - vector6;
									bool flag = false;
									int num12 = 1;
									while (num12 < 4 && num11 > 0.1f)
									{
										Vector3 vector7 = otherData.GetTargetPos (num12);
										Vector3 vector8 = Vector3.ClampMagnitude (vector7 - vector6, num11);
										if (Vector3.Dot (lhs2, vector8) > 0f)
										{
											vector7 = vector6 + vector8;
											float magnitude2 = vector8.magnitude;
											num11 -= magnitude2;
											Segment3 segment2 = new Segment3 (vector6, vector7);
											segment2.a.y = segment2.a.y * 0.5f;
											segment2.b.y = segment2.b.y * 0.5f;
											if (magnitude2 > 0.01f)
											{
												float num14;
												float num15;
												float num13;
												if (otherID < vehicleID)
												{
													num13 = segment2.DistanceSqr (segment, out num14, out num15);
												}
												else
												{
													num13 = segment.DistanceSqr (segment2, out num15, out num14);
												}
												if (num13 < 400f)
												{
													float num16 = num4 + magnitude * num15;
													float num17 = num10 + magnitude2 * num14 + 0.1f;
													if (num16 >= 0.01f && num16 * num2 > num17 * num)
													{
														float num18 = Vector3.Dot (lastFrameData.m_velocity, vector5) / magnitude;
														if (num16 >= 0.01f)
														{
															num16 -= num18 + 10f + otherData.Info.m_generatedInfo.m_size.z;
															float num19 = Mathf.Max (0f, CalculateMaxSpeed (num16, num18, maxBraking));
															if (num19 < 0.01f)
															{
																blocked = true;
															}
															maxSpeed = Mathf.Min (maxSpeed, num19);
														}
													}
													flag = true;
													break;
												}
											}
											lhs2 = vector8;
											num10 += magnitude2;
											vector6 = vector7;
										}
										num12++;
									}
									if (flag)
									{
										break;
									}
								}
							}
							lhs = vector5;
							num4 += magnitude;
							vector3 = vector4;
						}
					}
				}
			}
			return otherData.m_nextGridVehicle;
		}
		
		private void CheckOtherVehicles (ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ref float maxSpeed, ref bool blocked, float maxDistance, float maxBraking, int lodPhysics)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			Vector3 vector = ((Vector3)vehicleData.m_targetPos3) - frameData.m_position;
			Vector3 rhs = frameData.m_position + Vector3.ClampMagnitude (vector, maxDistance);
			Vector3 min = Vector3.Min (vehicleData.m_segment.Min (), rhs);
			Vector3 max = Vector3.Max (vehicleData.m_segment.Max (), rhs);
			int num = Mathf.Max ((int)((min.x - 100f) / 320f + 27f), 0);
			int num2 = Mathf.Max ((int)((min.z - 100f) / 320f + 27f), 0);
			int num3 = Mathf.Min ((int)((max.x + 100f) / 320f + 27f), 53);
			int num4 = Mathf.Min ((int)((max.z + 100f) / 320f + 27f), 53);
			for (int i = num2; i <= num4; i++)
			{
				for (int j = num; j <= num3; j++)
				{
					ushort num5 = instance.m_vehicleGrid2 [i * 54 + j];
					int num6 = 0;
					while (num5 != 0)
					{
						num5 = this.CheckOtherVehicle (vehicleID, ref vehicleData, ref frameData, ref maxSpeed, ref blocked, maxBraking, num5, ref instance.m_vehicles.m_buffer [(int)num5], min, max, lodPhysics);
						if (++num6 > 65536)
						{
							CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
							break;
						}
					}
				}
			}
		}
		private static ushort CheckOverlap (Segment3 segment, ushort ignoreVehicle, ushort otherID, ref Vehicle otherData, ref bool overlap)
		{
			float num;
			float num2;
			if ((ignoreVehicle == 0 || (otherID != ignoreVehicle && otherData.m_leadingVehicle != ignoreVehicle && otherData.m_trailingVehicle != ignoreVehicle)) && segment.DistanceSqr (otherData.m_segment, out num, out num2) < 400f)
			{
				overlap = true;
			}
			return otherData.m_nextGridVehicle;
		}
		
		private static bool CheckOverlap (Segment3 segment, ushort ignoreVehicle)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			Vector3 vector = segment.Min ();
			Vector3 vector2 = segment.Max ();
			int num = Mathf.Max ((int)((vector.x - 100f) / 320f + 27f), 0);
			int num2 = Mathf.Max ((int)((vector.z - 100f) / 320f + 27f), 0);
			int num3 = Mathf.Min ((int)((vector2.x + 100f) / 320f + 27f), 53);
			int num4 = Mathf.Min ((int)((vector2.z + 100f) / 320f + 27f), 53);
			bool result = false;
			for (int i = num2; i <= num4; i++)
			{
				for (int j = num; j <= num3; j++)
				{
					ushort num5 = instance.m_vehicleGrid2 [i * 54 + j];
					int num6 = 0;
					while (num5 != 0)
					{
						num5 = CheckOverlap (segment, ignoreVehicle, num5, ref instance.m_vehicles.m_buffer [(int)num5], ref result);
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
	}


}