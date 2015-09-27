using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using System;
using UnityEngine;

namespace Klyte.Unlimiter.Fake
{
	public class FakeCitizenAI
	{
		public CitizenInfo m_info;
		protected void CheckCollisions (ushort instanceID, ref CitizenInstance citizenData, Vector3 sourcePos, Vector3 targetPos, ushort buildingID, ref Vector3 pushAmount, ref float pushDivider)
		{
			Segment3 segment = new Segment3 (sourcePos, targetPos);
			Vector3 min = segment.Min ();
			min.x -= this.m_info.m_radius;
			min.z -= this.m_info.m_radius;
			Vector3 max = segment.Max ();
			max.x += this.m_info.m_radius;
			max.y += this.m_info.m_height;
			max.z += this.m_info.m_radius;
			CitizenManager instance = Singleton<CitizenManager>.instance;
			int num = Mathf.Max ((int)((min.x - 3f) / 8f + 1080f), 0);
			int num2 = Mathf.Max ((int)((min.z - 3f) / 8f + 1080f), 0);
			int num3 = Mathf.Min ((int)((max.x + 3f) / 8f + 1080f), 2159);
			int num4 = Mathf.Min ((int)((max.z + 3f) / 8f + 1080f), 2159);
			for (int i = num2; i <= num4; i++) {
				for (int j = num; j <= num3; j++) {
					ushort num5 = instance.m_citizenGrid [i * 2160 + j];
					int num6 = 0;
					while (num5 != 0) {
						num5 = this.CheckCollisions (instanceID, ref citizenData, segment, min, max, num5, ref instance.m_instances.m_buffer [(int)num5], ref pushAmount, ref pushDivider);
						if (++num6 > 65536) {
							CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
							break;
						}
					}
				}
			}
			VehicleManager instance2 = Singleton<VehicleManager>.instance;
			int num7 = Mathf.Max ((int)((min.x - 10f) / 32f + 270f), 0);
			int num8 = Mathf.Max ((int)((min.z - 10f) / 32f + 270f), 0);
			int num9 = Mathf.Min ((int)((max.x + 10f) / 32f + 270f), 539);
			int num10 = Mathf.Min ((int)((max.z + 10f) / 32f + 270f), 539);
			for (int k = num8; k <= num10; k++) {
				for (int l = num7; l <= num9; l++) {
					ushort num11 = instance2.m_vehicleGrid [k * 540 + l];
					int num12 = 0;
					while (num11 != 0) {
						num11 = this.CheckCollisions (instanceID, ref citizenData, segment, min, max, num11, ref instance2.m_vehicles.m_buffer [(int)num11], ref pushAmount, ref pushDivider);
						if (++num12 > 65536) {
							CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
							break;
						}
					}
				}
			}
			for (int m = num8; m <= num10; m++) {
				for (int n = num7; n <= num9; n++) {
					ushort num13 = instance2.m_parkedGrid [m * 540 + n];
					int num14 = 0;
					while (num13 != 0) {
						num13 = this.CheckCollisions (instanceID, ref citizenData, segment, min, max, num13, ref instance2.m_parkedVehicles.m_buffer [(int)num13], ref pushAmount, ref pushDivider);
						if (++num14 > 65536) {
							CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
							break;
						}
					}
				}
			}
			if (buildingID != 0) {
				BuildingManager instance3 = Singleton<BuildingManager>.instance;
				BuildingInfo info = instance3.m_buildings.m_buffer [(int)buildingID].Info;
				if (info.m_props != null) {
					Vector3 position = instance3.m_buildings.m_buffer [(int)buildingID].m_position;
					float angle = instance3.m_buildings.m_buffer [(int)buildingID].m_angle;
					int length = instance3.m_buildings.m_buffer [(int)buildingID].Length;
					Matrix4x4 matrix4x = default(Matrix4x4);
					matrix4x.SetTRS (Building.CalculateMeshPosition (info, position, angle, length), Quaternion.AngleAxis (angle * 57.29578f, Vector3.down), Vector3.one);
					for (int num15 = 0; num15 < info.m_props.Length; num15++) {
						BuildingInfo.Prop prop = info.m_props [num15];
						Randomizer randomizer = new Randomizer ((int)buildingID << 6 | prop.m_index);
						if (randomizer.Int32 (100u) < prop.m_probability && length >= prop.m_requiredLength) {
							Vector3 vector = matrix4x.MultiplyPoint (prop.m_position);
							if (vector.x >= min.x - 2f && vector.x <= max.x + 2f) {
								if (vector.z >= min.z - 2f && vector.z <= max.z + 2f) {
									PropInfo propInfo = prop.m_finalProp;
									TreeInfo treeInfo = prop.m_finalTree;
									float num16 = 0f;
									float num17 = 0f;
									if (propInfo != null) {
										propInfo = propInfo.GetVariation (ref randomizer);
										if (propInfo.m_isMarker || propInfo.m_isDecal || !propInfo.m_hasRenderer) {
											goto IL_7D3;
										}
										num16 = propInfo.m_generatedInfo.m_size.x * 0.5f;
										num17 = propInfo.m_generatedInfo.m_size.y;
									} else {
										if (treeInfo != null) {
											treeInfo = treeInfo.GetVariation (ref randomizer);
											num16 = (treeInfo.m_generatedInfo.m_size.x + treeInfo.m_generatedInfo.m_size.z) * 0.125f;
											num17 = treeInfo.m_generatedInfo.m_size.y;
										}
									}
									if (!prop.m_fixedHeight) {
										vector.y = Singleton<TerrainManager>.instance.SampleDetailHeight (vector);
									} else {
										if (info.m_requireHeightMap) {
											vector.y = Singleton<TerrainManager>.instance.SampleDetailHeight (vector) + prop.m_position.y;
										}
									}
									if (vector.y + num17 >= min.y && vector.y <= max.y) {
										num16 = this.m_info.m_radius + num16;
										float num19;
										float num18 = segment.DistanceSqr (vector, out num19);
										if (num18 < num16 * num16) {
											float num20 = num16 - Mathf.Sqrt (num18);
											float num21 = 1f - num18 / (num16 * num16);
											Vector3 a = segment.Position (num19 * 0.9f);
											a.y = 0f;
											vector.y = 0f;
											Vector3 vector2 = Vector3.Normalize (a - vector);
											Vector3 rhs = Vector3.Normalize (new Vector3 (segment.b.x - segment.a.x, 0f, segment.b.z - segment.a.z));
											Vector3 vector3 = new Vector3 (rhs.z, 0f, -rhs.x) * Mathf.Abs (Vector3.Dot (vector2, rhs) * 0.5f);
											if (Vector3.Dot (vector2, vector3) >= 0f) {
												vector2 += vector3;
											} else {
												vector2 -= vector3;
											}
											pushAmount += vector2 * (num20 * num21);
											pushDivider += num21;
										}
									}
								}
							}
						}
						IL_7D3:
						;
					}
				}
			}
		}
		private ushort CheckCollisions (ushort instanceID, ref CitizenInstance citizenData, Segment3 segment, Vector3 min, Vector3 max, ushort otherID, ref Vehicle otherData, ref Vector3 pushAmount, ref float pushDivider)
		{
			VehicleInfo info = otherData.Info;
			if (info.m_vehicleType == VehicleInfo.VehicleType.Bicycle)
			{
				return otherData.m_nextGridVehicle;
			}
			if ((otherData.m_flags & Vehicle.Flags.Transition) == Vehicle.Flags.None && (citizenData.m_flags & CitizenInstance.Flags.Transition) == CitizenInstance.Flags.None && (otherData.m_flags & Vehicle.Flags.Underground) != Vehicle.Flags.None != ((citizenData.m_flags & CitizenInstance.Flags.Underground) != CitizenInstance.Flags.None))
			{
				return otherData.m_nextGridVehicle;
			}
			Segment3 segment2 = otherData.m_segment;
			Vector3 vector = Vector3.Min (segment2.Min (), otherData.m_targetPos1);
			vector.x -= 1f;
			vector.z -= 1f;
			Vector3 vector2 = Vector3.Max (segment2.Max (), otherData.m_targetPos1);
			vector2.x += 1f;
			vector2.y += 1f;
			vector2.z += 1f;
			if (min.x < vector2.x && max.x > vector.x && min.z < vector2.z && max.z > vector.z && min.y < vector2.y && max.y > vector.y)
			{
				float num = this.m_info.m_radius + 1f;
				float num3;
				float t;
				float num2 = segment.DistanceSqr (segment2, out num3, out t);
				if (num2 < num * num)
				{
					float num4 = num - Mathf.Sqrt (num2);
					float num5 = 1f - num2 / (num * num);
					Vector3 a = segment.Position (num3 * 0.9f);
					Vector3 b = segment2.Position (t);
					a.y = 0f;
					b.y = 0f;
					Vector3 vector3 = Vector3.Normalize (a - b);
					Vector3 rhs = Vector3.Normalize (new Vector3 (segment.b.x - segment.a.x, 0f, segment.b.z - segment.a.z));
					Vector3 vector4 = new Vector3 (rhs.z, 0f, -rhs.x) * Mathf.Abs (Vector3.Dot (vector3, rhs) * 0.5f);
					if (Vector3.Dot (vector3, vector4) >= 0f)
					{
						vector3 += vector4;
					}
					else
					{
						vector3 -= vector4;
					}
					pushAmount += vector3 * (num4 * num5);
					pushDivider += num5;
				}
				float magnitude = otherData.GetLastFrameVelocity ().magnitude;
				if (magnitude > 0.1f)
				{
					float num6 = this.m_info.m_radius + 3f;
					segment2.a = segment2.b;
					segment2.b += Vector3.ClampMagnitude (((Vector3)otherData.m_targetPos1) - segment2.b, magnitude * 4f);
					num2 = segment.DistanceSqr (segment2, out num3, out t);
					if (num2 > num * num && num2 < num6 * num6)
					{
						float num7 = num6 - Mathf.Sqrt (num2);
						float num8 = 1f - num2 / (num6 * num6);
						Vector3 a2 = segment.Position (num3 * 0.9f);
						Vector3 b2 = segment2.Position (t);
						a2.y = 0f;
						b2.y = 0f;
						Vector3 vector5 = a2 - b2;
						pushAmount += vector5.normalized * (num7 * num8);
						pushDivider += num8;
					}
				}
			}
			return otherData.m_nextGridVehicle;
		}
		private ushort CheckCollisions (ushort instanceID, ref CitizenInstance citizenData, Segment3 segment, Vector3 min, Vector3 max, ushort otherID, ref CitizenInstance otherData, ref Vector3 pushAmount, ref float pushDivider)
		{
			if (otherID == instanceID)
			{
				return otherData.m_nextGridInstance;
			}
			if (((citizenData.m_flags | otherData.m_flags) & CitizenInstance.Flags.Transition) == CitizenInstance.Flags.None && (citizenData.m_flags & CitizenInstance.Flags.Underground) != (citizenData.m_flags & CitizenInstance.Flags.Underground))
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
			if (min.x < vector2.x && max.x > vector.x && min.z < vector2.z && max.z > vector.z && min.y < vector2.y && max.y > vector.y)
			{
				float num = this.m_info.m_radius + info.m_radius;
				float num3;
				float t;
				float num2 = segment.DistanceSqr (segment2, out num3, out t);
				if (num2 < num * num)
				{
					float num4 = num - Mathf.Sqrt (num2);
					float num5 = 1f - num2 / (num * num);
					Vector3 a = segment.Position (num3 * 0.9f);
					Vector3 b2 = segment2.Position (t);
					a.y = 0f;
					b2.y = 0f;
					Vector3 vector3 = a - b2;
					Vector3 vector4 = new Vector3 (segment.b.z - segment.a.z, 0f, segment.a.x - segment.b.x);
					if (Vector3.Dot (vector3, vector4) >= 0f)
					{
						vector3 += vector4;
					}
					else
					{
						vector3 -= vector4;
					}
					pushAmount += vector3.normalized * (num4 * num5);
					pushDivider += num5;
				}
			}
			return otherData.m_nextGridInstance;
		}
		private ushort CheckCollisions (ushort instanceID, ref CitizenInstance citizenData, Segment3 segment, Vector3 min, Vector3 max, ushort otherID, ref VehicleParked otherData, ref Vector3 pushAmount, ref float pushDivider)
		{
			VehicleInfo info = otherData.Info;
			Vector3 position = otherData.m_position;
			Vector3 b = otherData.m_rotation * new Vector3 (0f, 0f, Mathf.Max (0.5f, info.m_generatedInfo.m_size.z * 0.5f - 1f));
			Segment3 segment2;
			segment2.a = position - b;
			segment2.b = position + b;
			Vector3 vector = segment2.Min ();
			vector.x -= 1f;
			vector.z -= 1f;
			Vector3 vector2 = segment2.Max ();
			vector2.x += 1f;
			vector2.y += 1f;
			vector2.z += 1f;
			if (min.x < vector2.x && max.x > vector.x && min.z < vector2.z && max.z > vector.z && min.y < vector2.y && max.y > vector.y)
			{
				float num = this.m_info.m_radius + 1f;
				float num3;
				float t;
				float num2 = segment.DistanceSqr (segment2, out num3, out t);
				if (num2 < num * num)
				{
					float num4 = num - Mathf.Sqrt (num2);
					float num5 = 1f - num2 / (num * num);
					Vector3 a = segment.Position (num3 * 0.9f);
					Vector3 b2 = segment2.Position (t);
					a.y = 0f;
					b2.y = 0f;
					Vector3 vector3 = Vector3.Normalize (a - b2);
					Vector3 rhs = Vector3.Normalize (new Vector3 (segment.b.x - segment.a.x, 0f, segment.b.z - segment.a.z));
					Vector3 vector4 = new Vector3 (rhs.z, 0f, -rhs.x) * Mathf.Abs (Vector3.Dot (vector3, rhs) * 0.5f);
					if (Vector3.Dot (vector3, vector4) >= 0f)
					{
						vector3 += vector4;
					}
					else
					{
						vector3 -= vector4;
					}
					pushAmount += vector3 * (num4 * num5);
					pushDivider += num5;
				}
			}
			return otherData.m_nextGridParked;
		}
		

	}
}

