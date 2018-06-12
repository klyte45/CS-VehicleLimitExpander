using ColossalFramework;
using ColossalFramework.Math;
using System;
using UnityEngine;

namespace Klyte.Unlimiter.Fake
{
	public class FakeCarAI : VehicleAI
	{
		protected virtual void PathfindFailure (ushort vehicleID, ref Vehicle data)
		{
			data.Unspawn (vehicleID);
		}

		protected virtual void PathfindSuccess (ushort vehicleID, ref Vehicle data)
		{
		}

		public override void SimulationStep (ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
		{
			if ((data.m_flags & Vehicle.Flags.WaitingPath) != Vehicle.Flags.None) {
				PathManager instance = Singleton<PathManager>.instance;
				byte pathFindFlags = instance.m_pathUnits.m_buffer [(int)((UIntPtr)data.m_path)].m_pathFindFlags;
				if ((pathFindFlags & 4) != 0) {
					data.m_pathPositionIndex = 255;
					data.m_flags &= ~Vehicle.Flags.WaitingPath;
					data.m_flags &= ~Vehicle.Flags.Arriving;
					this.PathfindSuccess (vehicleID, ref data);
					this.TrySpawn (vehicleID, ref data);
				} else {
					if ((pathFindFlags & 8) != 0) {
						data.m_flags &= ~Vehicle.Flags.WaitingPath;
						Singleton<PathManager>.instance.ReleasePath (data.m_path);
						data.m_path = 0u;
						this.PathfindFailure (vehicleID, ref data);
						return;
					}
				}
			} else {
				if ((data.m_flags & Vehicle.Flags.WaitingSpace) != Vehicle.Flags.None) {
					this.TrySpawn (vehicleID, ref data);
				}
			}
			Vector3 lastFramePosition = data.GetLastFramePosition ();
			int lodPhysics;
			if (Vector3.SqrMagnitude (physicsLodRefPos - lastFramePosition) >= 1210000f) {
				lodPhysics = 2;
			} else {
				if (Vector3.SqrMagnitude (Singleton<SimulationManager>.instance.m_simulationView.m_position - lastFramePosition) >= 250000f) {
					lodPhysics = 1;
				} else {
					lodPhysics = 0;
				}
			}
			this.SimulationStep (vehicleID, ref data, vehicleID, ref data, lodPhysics);
			if (data.m_leadingVehicle == 0 && data.m_trailingVehicle != 0) {
				VehicleManager instance2 = Singleton<VehicleManager>.instance;
				ushort num = data.m_trailingVehicle;
				int num2 = 0;
				while (num != 0) {
					ushort trailingVehicle = instance2.m_vehicles.m_buffer [(int)num].m_trailingVehicle;
					VehicleInfo info = instance2.m_vehicles.m_buffer [(int)num].Info;
					info.m_vehicleAI.SimulationStep (num, ref instance2.m_vehicles.m_buffer [(int)num], vehicleID, ref data, lodPhysics);
					num = trailingVehicle;
					if (++num2 > 65536) {
						CODebugBase<LogChannel>.Error (LogChannel.Core, "Liste invalide détectée !\n" + Environment.StackTrace);
						break;
					}
				}
			}
			int privateServiceIndex = ItemClass.GetPrivateServiceIndex (this.m_info.m_class.m_service);
			int num3 = (privateServiceIndex == -1) ? 150 : 100;
			if ((data.m_flags & (Vehicle.Flags.Spawned | Vehicle.Flags.WaitingPath | Vehicle.Flags.WaitingSpace)) == Vehicle.Flags.None && data.m_cargoParent == 0) {
				Singleton<VehicleManager>.instance.ReleaseVehicle (vehicleID);
			} else {
				if ((int)data.m_blockCounter == num3) {
					Singleton<VehicleManager>.instance.ReleaseVehicle (vehicleID);
				}
			}
		}

		private void CheckOtherVehicles (ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ref float maxSpeed, ref bool blocked, ref Vector3 collisionPush, float maxDistance, float maxBraking, int lodPhysics)
		{
			Vector3 vector = ((Vector3)vehicleData.m_targetPos3) - frameData.m_position;
			Vector3 rhs = frameData.m_position + Vector3.ClampMagnitude (vector, maxDistance);
			Vector3 min = Vector3.Min (vehicleData.m_segment.Min (), rhs);
			Vector3 max = Vector3.Max (vehicleData.m_segment.Max (), rhs);
			VehicleManager instance = Singleton<VehicleManager>.instance;
			int num = Mathf.Max ((int)((min.x - 10f) / 32f + 270f), 0);
			int num2 = Mathf.Max ((int)((min.z - 10f) / 32f + 270f), 0);
			int num3 = Mathf.Min ((int)((max.x + 10f) / 32f + 270f), 539);
			int num4 = Mathf.Min ((int)((max.z + 10f) / 32f + 270f), 539);
			for (int i = num2; i <= num4; i++) {
				for (int j = num; j <= num3; j++) {
					ushort num5 = instance.m_vehicleGrid [i * 540 + j];
					int num6 = 0;
					while (num5 != 0) {
						num5 = this.CheckOtherVehicle (vehicleID, ref vehicleData, ref frameData, ref maxSpeed, ref blocked, ref collisionPush, maxBraking, num5, ref instance.m_vehicles.m_buffer [(int)num5], min, max, lodPhysics);
						if (++num6 > 65536) {
							CODebugBase<LogChannel>.Error (LogChannel.Core, "Liste invalide détectée !\n" + Environment.StackTrace);
							break;
						}
					}
				}
			}
			if (lodPhysics == 0) {
				CitizenManager instance2 = Singleton<CitizenManager>.instance;
				float num7 = 0f;
				Vector3 vector2 = vehicleData.m_segment.b;
				Vector3 lhs = vehicleData.m_segment.b - vehicleData.m_segment.a;
				for (int k = 0; k < 4; k++) {
					Vector3 vector3 = vehicleData.GetTargetPos (k);
					Vector3 vector4 = vector3 - vector2;
					if (Vector3.Dot (lhs, vector4) > 0f) {
						float magnitude = vector4.magnitude;
						if (magnitude > 0.01f) {
							Segment3 segment = new Segment3 (vector2, vector3);
							min = segment.Min ();
							max = segment.Max ();
							int num8 = Mathf.Max ((int)((min.x - 3f) / 8f + 1080f), 0);
							int num9 = Mathf.Max ((int)((min.z - 3f) / 8f + 1080f), 0);
							int num10 = Mathf.Min ((int)((max.x + 3f) / 8f + 1080f), 2159);
							int num11 = Mathf.Min ((int)((max.z + 3f) / 8f + 1080f), 2159);
							for (int l = num9; l <= num11; l++) {
								for (int m = num8; m <= num10; m++) {
									ushort num12 = instance2.m_citizenGrid [l * 2160 + m];
									int num13 = 0;
									while (num12 != 0) {
										num12 = this.CheckCitizen (vehicleID, ref vehicleData, segment, num7, magnitude, ref maxSpeed, ref blocked, maxBraking, num12, ref instance2.m_instances.m_buffer [(int)num12], min, max);
										if (++num13 > 65536) {
											CODebugBase<LogChannel>.Error (LogChannel.Core, "Liste invalide détectée !\n" + Environment.StackTrace);
											break;
										}
									}
								}
							}
						}
						lhs = vector4;
						num7 += magnitude;
						vector2 = vector3;
					}
				}
			}
		}

		private ushort CheckCitizen (ushort vehicleID, ref Vehicle vehicleData, Segment3 segment, float lastLen, float nextLen, ref float maxSpeed, ref bool blocked, float maxBraking, ushort otherID, ref CitizenInstance otherData, Vector3 min, Vector3 max)
		{
			if ((vehicleData.m_flags & Vehicle.Flags.Transition) == Vehicle.Flags.None && (otherData.m_flags & CitizenInstance.Flags.Transition) == CitizenInstance.Flags.None && (vehicleData.m_flags & Vehicle.Flags.Underground) != Vehicle.Flags.None != ((otherData.m_flags & CitizenInstance.Flags.Underground) != CitizenInstance.Flags.None))
			{
				return otherData.m_nextGridInstance;
			}
			CitizenInfo info = otherData.Info;
			CitizenInstance.Frame lastFrameData = otherData.GetLastFrameData ();
			Vector3 position = lastFrameData.m_position;
			Vector3 b = lastFrameData.m_position + lastFrameData.m_velocity;
			Segment3 segment2 = new Segment3 (position, b);
			Vector3 vector = segment2.Min ();
			vector.x -= info.m_radius;
			vector.z -= info.m_radius;
			Vector3 vector2 = segment2.Max ();
			vector2.x += info.m_radius;
			vector2.y += info.m_height;
			vector2.z += info.m_radius;
			float num;
			float num2;
			if (min.x < vector2.x + 1f && min.y < vector2.y && min.z < vector2.z + 1f && vector.x < max.x + 1f && vector.y < max.y + 2f && vector.z < max.z + 1f && segment.DistanceSqr (segment2, out num, out num2) < (1f + info.m_radius) * (1f + info.m_radius))
			{
				float num3 = lastLen + nextLen * num;
				if (num3 >= 0.01f)
				{
					num3 -= 2f;
					float b2 = Mathf.Max (1f, CalculateMaxSpeed (num3, 0f, maxBraking));
					maxSpeed = Mathf.Min (maxSpeed, b2);
				}
			}
			return otherData.m_nextGridInstance;
		}

		private static float CalculateMaxSpeed (float targetDistance, float targetSpeed, float maxBraking)
		{
			float num = 0.5f * maxBraking;
			float num2 = num + targetSpeed;
			return Mathf.Sqrt (Mathf.Max (0f, num2 * num2 + 2f * targetDistance * maxBraking)) - num;
		}



		private ushort CheckOtherVehicle (ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ref float maxSpeed, ref bool blocked, ref Vector3 collisionPush, float maxBraking, ushort otherID, ref Vehicle otherData, Vector3 min, Vector3 max, int lodPhysics)
		{
			if (otherID != vehicleID && vehicleData.m_leadingVehicle != otherID && vehicleData.m_trailingVehicle != otherID)
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
				Vector3 vector;
				Vector3 vector2;
				if (lodPhysics >= 2)
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
					if (lodPhysics < 2)
					{
						float num2;
						float num3;
						float num = vehicleData.m_segment.DistanceSqr (otherData.m_segment, out num2, out num3);
						if (num < 4f)
						{
							Vector3 a = vehicleData.m_segment.Position (0.5f);
							Vector3 b = otherData.m_segment.Position (0.5f);
							Vector3 lhs = vehicleData.m_segment.b - vehicleData.m_segment.a;
							if (Vector3.Dot (lhs, a - b) < 0f)
							{
								collisionPush -= lhs.normalized * (0.1f - num * 0.025f);
							}
							else
							{
								collisionPush += lhs.normalized * (0.1f - num * 0.025f);
							}
							blocked = true;
						}
					}
					float num4 = frameData.m_velocity.magnitude + 0.01f;
					float num5 = lastFrameData.m_velocity.magnitude;
					float num6 = num5 * (0.5f + 0.5f * num5 / info.m_braking) + Mathf.Min (1f, num5);
					num5 += 0.01f;
					float num7 = 0f;
					Vector3 vector3 = vehicleData.m_segment.b;
					Vector3 lhs2 = vehicleData.m_segment.b - vehicleData.m_segment.a;
					for (int i = 0; i < 4; i++)
					{
						Vector3 vector4 = vehicleData.GetTargetPos (i);
						Vector3 vector5 = vector4 - vector3;
						if (Vector3.Dot (lhs2, vector5) > 0f)
						{
							float magnitude = vector5.magnitude;
							Segment3 segment = new Segment3 (vector3, vector4);
							min = segment.Min ();
							max = segment.Max ();
							segment.a.y = segment.a.y * 0.5f;
							segment.b.y = segment.b.y * 0.5f;
							if (magnitude > 0.01f && min.x < vector2.x + 2f && min.y < vector2.y + 2f && min.z < vector2.z + 2f && vector.x < max.x + 2f && vector.y < max.y + 2f && vector.z < max.z + 2f)
							{
								Vector3 a2 = otherData.m_segment.a;
								a2.y *= 0.5f;
								float num8;
								if (segment.DistanceSqr (a2, out num8) < 4f)
								{
									float num9 = Vector3.Dot (lastFrameData.m_velocity, vector5) / magnitude;
									float num10 = num7 + magnitude * num8;
									if (num10 >= 0.01f)
									{
										num10 -= num9 + 3f;
										float num11 = Mathf.Max (0f, CalculateMaxSpeed (num10, num9, maxBraking));
										if (num11 < 0.01f)
										{
											blocked = true;
										}
										Vector3 rhs = Vector3.Normalize (((Vector3)otherData.m_targetPos0 ) - otherData.m_segment.a);
										float num12 = 1.2f - 1f / ((float)vehicleData.m_blockCounter * 0.02f + 0.5f);
										if (Vector3.Dot (vector5, rhs) > num12 * magnitude)
										{
											maxSpeed = Mathf.Min (maxSpeed, num11);
										}
									}
									break;
								}
								if (lodPhysics < 2)
								{
									float num13 = 0f;
									float num14 = num6;
									Vector3 vector6 = otherData.m_segment.b;
									Vector3 lhs3 = otherData.m_segment.b - otherData.m_segment.a;
									bool flag = false;
									int num15 = 0;
									while (num15 < 4 && num14 > 0.1f)
									{
										Vector3 vector7 = otherData.GetTargetPos (num15);
										Vector3 vector8 = Vector3.ClampMagnitude (vector7 - vector6, num14);
										if (Vector3.Dot (lhs3, vector8) > 0f)
										{
											vector7 = vector6 + vector8;
											float magnitude2 = vector8.magnitude;
											num14 -= magnitude2;
											Segment3 segment2 = new Segment3 (vector6, vector7);
											segment2.a.y = segment2.a.y * 0.5f;
											segment2.b.y = segment2.b.y * 0.5f;
											if (magnitude2 > 0.01f)
											{
												float num17;
												float num18;
												float num16;
												if (otherID < vehicleID)
												{
													num16 = segment2.DistanceSqr (segment, out num17, out num18);
												}
												else
												{
													num16 = segment.DistanceSqr (segment2, out num18, out num17);
												}
												if (num16 < 4f)
												{
													float num19 = num7 + magnitude * num18;
													float num20 = num13 + magnitude2 * num17 + 0.1f;
													if (num19 >= 0.01f && num19 * num5 > num20 * num4)
													{
														float num21 = Vector3.Dot (lastFrameData.m_velocity, vector5) / magnitude;
														if (num19 >= 0.01f)
														{
															num19 -= num21 + 1f + otherData.Info.m_generatedInfo.m_size.z;
															float num22 = Mathf.Max (0f, CalculateMaxSpeed (num19, num21, maxBraking));
															if (num22 < 0.01f)
															{
																blocked = true;
															}
															maxSpeed = Mathf.Min (maxSpeed, num22);
														}
													}
													flag = true;
													break;
												}
											}
											lhs3 = vector8;
											num13 += magnitude2;
											vector6 = vector7;
										}
										num15++;
									}
									if (flag)
									{
										break;
									}
								}
							}
							lhs2 = vector5;
							num7 += magnitude;
							vector3 = vector4;
						}
					}
				}
			}
			return otherData.m_nextGridVehicle;
		}
		
		private static ushort CheckOverlap (Segment3 segment, ushort ignoreVehicle, float maxVelocity, ushort otherID, ref Vehicle otherData, ref bool overlap)
		{
			float num;
			float num2;
			if ((ignoreVehicle == 0 || (otherID != ignoreVehicle && otherData.m_leadingVehicle != ignoreVehicle && otherData.m_trailingVehicle != ignoreVehicle)) && segment.DistanceSqr (otherData.m_segment, out num, out num2) < 4f)
			{
				VehicleInfo info = otherData.Info;
				if (info.m_vehicleType == VehicleInfo.VehicleType.Bicycle)
				{
					return otherData.m_nextGridVehicle;
				}
				if (otherData.GetLastFrameData ().m_velocity.sqrMagnitude < maxVelocity * maxVelocity)
				{
					overlap = true;
				}
			}
			return otherData.m_nextGridVehicle;
		}


		private static bool CheckOverlap (Segment3 segment, ushort ignoreVehicle, float maxVelocity)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			Vector3 vector = segment.Min ();
			Vector3 vector2 = segment.Max ();
			int num = Mathf.Max ((int)((vector.x - 10f) / 32f + 270f), 0);
			int num2 = Mathf.Max ((int)((vector.z - 10f) / 32f + 270f), 0);
			int num3 = Mathf.Min ((int)((vector2.x + 10f) / 32f + 270f), 539);
			int num4 = Mathf.Min ((int)((vector2.z + 10f) / 32f + 270f), 539);
			bool result = false;
			for (int i = num2; i <= num4; i++) {
				for (int j = num; j <= num3; j++) {
					ushort num5 = instance.m_vehicleGrid [i * 540 + j];
					int num6 = 0;
					while (num5 != 0) {
						num5 = CheckOverlap (segment, ignoreVehicle, maxVelocity, num5, ref instance.m_vehicles.m_buffer [(int)num5], ref result);
						if (++num6 > 65536) {
							CODebugBase<LogChannel>.Error (LogChannel.Core, "List invalide détectée !\n" + Environment.StackTrace);
							break;
						}
					}
				}
			}
			return result;
		}

	}
}
