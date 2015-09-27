using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.Math;
using ColossalFramework.Globalization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using Klyte.Unlimiter.Attributes;

namespace Klyte.Unlimiter.Fake
{
	public class FakeVehicleManager
	{
		//
		// Static Fields
		//
		public const float VEHICLEGRID_CELL_SIZE2 = 320f;
		public const int VEHICLEGRID_RESOLUTION2 = 54;
		public const int MAX_VEHICLE_COUNT = 65536;
		public const int MAX_PARKED_COUNT = 32768;
		public const float VEHICLEGRID_CELL_SIZE = 32f;
		public const int VEHICLEGRID_RESOLUTION = 540;
		public static FakeVehicleManager instance;
		//
		// Fields
		//
		[NonSerialized]
		public int[]
			ID_VehicleTransform;
		[NonSerialized]
		public int[]
			ID_VehicleLightState;
		[NonSerialized]
		public int[]
			ID_VehicleColor;
		[NonSerialized]
		public int
			ID_AtlasRect;
		[NonSerialized]
		public int
			ID_MainTex;
		[NonSerialized]
		public int
			ID_XYSMap;
		[NonSerialized]
		public int
			ID_ACIMap;
		[NonSerialized]
		public AudioGroup
			m_audioGroup;
		private bool m_vehiclesRefreshed;
		private ulong[] m_renderBuffer;
		private ulong[] m_renderBuffer2;
		private FastList<ushort>[] m_transferVehicles;
		[NonSerialized]
		public int
			m_undergroundLayer;
		public Texture2D m_lodRgbAtlas;
		public Texture2D m_lodXysAtlas;
		[NonSerialized]
		public int
			ID_Color;
		[NonSerialized]
		public Array16<Vehicle>
			m_vehicles;
		[NonSerialized]
		public Array16<VehicleParked>
			m_parkedVehicles;
		[NonSerialized]
		public ulong[]
			m_updatedParked;
		public int m_infoCount;
		public Texture2D m_lodAciAtlas;
		public int m_vehicleCount;
		public int m_parkedCount;
		[NonSerialized]
		public bool
			m_parkedUpdated;
		[NonSerialized]
		public int
			ID_TyreMatrix;
		[NonSerialized]
		public MaterialPropertyBlock
			m_materialBlock;
		[NonSerialized]
		public int
			ID_LightState;
		[NonSerialized]
		public int
			ID_TyrePosition;
		[NonSerialized]
		public ushort[]
			m_vehicleGrid2;
		[NonSerialized]
		public ushort[]
			m_parkedGrid;
		[NonSerialized]
		public ushort[]
			m_vehicleGrid;
	
		//
		// Static Methods
		//
		private static int GetTransferIndex (ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level)
		{
			int num;
			if (subService != ItemClass.SubService.None) {
				num = 20 + subService - ItemClass.SubService.ResidentialLow;
			} else {
				num = service - ItemClass.Service.Residential;
			}
			return (int)(num * 5 + level);
		}
	
		//
		// Methods
		//
		public void AddToGrid (ushort parked, ref VehicleParked data)
		{
			int gridX = Mathf.Clamp ((int)(data.m_position.x / 32f + 270f), 0, 539);
			int gridZ = Mathf.Clamp ((int)(data.m_position.z / 32f + 270f), 0, 539);
			this.AddToGrid (parked, ref data, gridX, gridZ);
		}
	
		public void AddToGrid (ushort vehicle, ref Vehicle data, bool large)
		{
			Vector3 lastFramePosition = data.GetLastFramePosition ();
			if (large) {
				int gridX = Mathf.Clamp ((int)(lastFramePosition.x / 320f + 27f), 0, 53);
				int gridZ = Mathf.Clamp ((int)(lastFramePosition.z / 320f + 27f), 0, 53);
				this.AddToGrid (vehicle, ref data, large, gridX, gridZ);
			} else {
				int gridX2 = Mathf.Clamp ((int)(lastFramePosition.x / 32f + 270f), 0, 539);
				int gridZ2 = Mathf.Clamp ((int)(lastFramePosition.z / 32f + 270f), 0, 539);
				this.AddToGrid (vehicle, ref data, large, gridX2, gridZ2);
			}
		}
	
		public void AddToGrid (ushort vehicle, ref Vehicle data, bool large, int gridX, int gridZ)
		{
			if (large) {
				int num = gridZ * 54 + gridX;
				data.m_nextGridVehicle = Singleton<VehicleManager>.instance.m_vehicleGrid2 [num];
				Singleton<VehicleManager>.instance.m_vehicleGrid2 [num] = vehicle;
			} else {
				int num2 = gridZ * 540 + gridX;
				data.m_nextGridVehicle = Singleton<VehicleManager>.instance.m_vehicleGrid [num2];
				Singleton<VehicleManager>.instance.m_vehicleGrid [num2] = vehicle;
			}
		}
	
		public void AddToGrid (ushort parked, ref VehicleParked data, int gridX, int gridZ)
		{
			int num = gridZ * 540 + gridX;
			data.m_nextGridParked = Singleton<VehicleManager>.instance.m_parkedGrid [num];
			Singleton<VehicleManager>.instance.m_parkedGrid [num] = parked;
		}
	
		public static void Init ()
		{
			instance = new FakeVehicleManager ();
			Array16<Vehicle> vehiclesOriginal = VehicleManager.instance.m_vehicles;
			VehicleManager.instance.m_vehicles = new Array16<Vehicle> (65536);
			ushort j;			
			VehicleManager.instance.m_vehicles.CreateItem (out j);
			VehicleManager.instance.m_vehicles.ClearUnused ();
			for (int i= 0; i<VehicleManager.instance.m_vehicles.m_buffer.Length; i++) {
				if ( i< vehiclesOriginal.m_buffer.Length && vehiclesOriginal.m_buffer[i].m_flags != Vehicle.Flags.None) {
					VehicleManager.instance.m_vehicles.m_buffer [i] = vehiclesOriginal.m_buffer [i];
				} else {
					VehicleManager.instance.m_vehicles.ReleaseItem((ushort) i);
				}
			}

			instance.m_renderBuffer = new ulong[1024];
			var prop = VehicleManager.instance.GetType ().GetField ("m_renderBuffer", System.Reflection.BindingFlags.NonPublic
				| System.Reflection.BindingFlags.Instance);
			prop.SetValue (VehicleManager.instance, new ulong[1024]);
			Singleton<VehicleManager>.instance.m_vehicleGrid = new ushort[291600];
			Singleton<VehicleManager>.instance.m_vehicleGrid2 = new ushort[2916];
			Singleton<VehicleManager>.instance.m_parkedGrid = new ushort[291600];
			instance.m_transferVehicles = new FastList<ushort>[195];
			Singleton<VehicleManager>.instance.m_materialBlock = new MaterialPropertyBlock ();
			instance.ID_TyreMatrix = Shader.PropertyToID ("_TyreMatrix");
			instance.ID_TyrePosition = Shader.PropertyToID ("_TyrePosition");
			instance.ID_LightState = Shader.PropertyToID ("_LightState");
			instance.ID_Color = Shader.PropertyToID ("_Color");
			instance.ID_MainTex = Shader.PropertyToID ("_MainTex");
			instance.ID_XYSMap = Shader.PropertyToID ("_XYSMap");
			instance.ID_ACIMap = Shader.PropertyToID ("_ACIMap");
			instance.ID_AtlasRect = Shader.PropertyToID ("_AtlasRect");
			instance.ID_VehicleTransform = new int[16];
			instance.ID_VehicleLightState = new int[16];
			instance.ID_VehicleColor = new int[16];
			for (int i = 0; i < 16; i++) {
				instance.ID_VehicleTransform [i] = Shader.PropertyToID ("_VehicleTransform" + i);
				instance.ID_VehicleLightState [i] = Shader.PropertyToID ("_VehicleLightState" + i);
				instance.ID_VehicleColor [i] = Shader.PropertyToID ("_VehicleColor" + i);
			}
			Singleton<VehicleManager>.instance.m_audioGroup = new AudioGroup (5, new SavedFloat (Settings.effectAudioVolume, Settings.gameSettingsFile, DefaultSettings.effectAudioVolume, true));
			Singleton<VehicleManager>.instance.m_undergroundLayer = LayerMask.NameToLayer ("MetroTunnels");
			ushort num;
			Singleton<VehicleManager>.instance.m_vehicles.CreateItem (out num);
			Singleton<VehicleManager>.instance.m_parkedVehicles.CreateItem (out num);
		}
	
		public bool CreateParkedVehicle (out ushort parked, ref Randomizer r, VehicleInfo info, Vector3 position, Quaternion rotation, uint ownerCitizen)
		{
			ushort num;
			if (Singleton<VehicleManager>.instance.m_parkedVehicles.CreateItem (out num, ref r)) {
				parked = num;
				Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)parked].m_flags = 9;
				Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)parked].Info = info;
				Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)parked].m_position = position;
				Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)parked].m_rotation = rotation;
				Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)parked].m_ownerCitizen = ownerCitizen;
				Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)parked].m_travelDistance = 0f;
				Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)parked].m_nextGridParked = 0;
				this.AddToGrid (parked, ref Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)parked]);
				Singleton<VehicleManager>.instance.m_parkedCount = (int)(Singleton<VehicleManager>.instance.m_parkedVehicles.ItemCount () - 1u);
				return true;
			}
			parked = 0;
			return false;
		}
	
		public bool CreateVehicle (out ushort vehicle, ref Randomizer r, VehicleInfo info, Vector3 position, TransferManager.TransferReason type, bool transferToSource, bool transferToTarget)
		{
			ushort num;
			if (Singleton<VehicleManager>.instance.m_vehicles.CreateItem (out num, ref r)) {
				vehicle = num;
				Vehicle.Frame frame = new Vehicle.Frame (position, Quaternion.identity);
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_flags = Vehicle.Flags.Created;
				if (transferToSource) {
					Vehicle[] expr_55_cp_0 = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;
					ushort expr_55_cp_1 = vehicle;
					expr_55_cp_0 [(int)expr_55_cp_1].m_flags = (expr_55_cp_0 [(int)expr_55_cp_1].m_flags | Vehicle.Flags.TransferToSource);
				}
				if (transferToTarget) {
					Vehicle[] expr_7C_cp_0 = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;
					ushort expr_7C_cp_1 = vehicle;
					expr_7C_cp_0 [(int)expr_7C_cp_1].m_flags = (expr_7C_cp_0 [(int)expr_7C_cp_1].m_flags | Vehicle.Flags.TransferToTarget);
				}
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].Info = info;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_frame0 = frame;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_frame1 = frame;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_frame2 = frame;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_frame3 = frame;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_targetPos0 = Vector4.zero;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_targetPos1 = Vector4.zero;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_targetPos2 = Vector4.zero;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_targetPos3 = Vector4.zero;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_sourceBuilding = 0;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_targetBuilding = 0;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_transferType = (byte)type;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_transferSize = 0;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_waitCounter = 0;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_blockCounter = 0;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_nextGridVehicle = 0;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_nextOwnVehicle = 0;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_nextGuestVehicle = 0;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_nextLineVehicle = 0;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_transportLine = 0;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_leadingVehicle = 0;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_trailingVehicle = 0;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_cargoParent = 0;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_firstCargo = 0;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_nextCargo = 0;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_citizenUnits = 0u;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_path = 0u;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_lastFrame = 0;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_pathPositionIndex = 0;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_lastPathOffset = 0;
				Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_gateIndex = 0;
				info.m_vehicleAI.CreateVehicle (vehicle, ref Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle]);
				info.m_vehicleAI.FrameDataUpdated (vehicle, ref Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle], ref Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle].m_frame0);
				Singleton<VehicleManager>.instance.m_vehicleCount = (int)(Singleton<VehicleManager>.instance.m_vehicles.ItemCount () - 1u);
				return true;
			}
			vehicle = 0;
			return false;
		}
	
		protected void EndRenderingImpl (RenderManager.CameraInfo cameraInfo)
		{
			float levelOfDetailFactor = RenderManager.LevelOfDetailFactor;
			float near = cameraInfo.m_near;
			float d = Mathf.Min (levelOfDetailFactor * 5000f, Mathf.Min (levelOfDetailFactor * 2000f + cameraInfo.m_height * 0.6f, cameraInfo.m_far));
			Vector3 lhs = cameraInfo.m_position + cameraInfo.m_directionA * near;
			Vector3 rhs = cameraInfo.m_position + cameraInfo.m_directionB * near;
			Vector3 lhs2 = cameraInfo.m_position + cameraInfo.m_directionC * near;
			Vector3 rhs2 = cameraInfo.m_position + cameraInfo.m_directionD * near;
			Vector3 lhs3 = cameraInfo.m_position + cameraInfo.m_directionA * d;
			Vector3 rhs3 = cameraInfo.m_position + cameraInfo.m_directionB * d;
			Vector3 lhs4 = cameraInfo.m_position + cameraInfo.m_directionC * d;
			Vector3 rhs4 = cameraInfo.m_position + cameraInfo.m_directionD * d;
			Vector3 vector = Vector3.Min (Vector3.Min (Vector3.Min (lhs, rhs), Vector3.Min (lhs2, rhs2)), Vector3.Min (Vector3.Min (lhs3, rhs3), Vector3.Min (lhs4, rhs4)));
			Vector3 vector2 = Vector3.Max (Vector3.Max (Vector3.Max (lhs, rhs), Vector3.Max (lhs2, rhs2)), Vector3.Max (Vector3.Max (lhs3, rhs3), Vector3.Max (lhs4, rhs4)));
			int num = Mathf.Max ((int)((vector.x - 10f) / 32f + 270f), 0);
			int num2 = Mathf.Max ((int)((vector.z - 10f) / 32f + 270f), 0);
			int num3 = Mathf.Min ((int)((vector2.x + 10f) / 32f + 270f), 539);
			int num4 = Mathf.Min ((int)((vector2.z + 10f) / 32f + 270f), 539);
			for (int i = num2; i <= num4; i++) {
				for (int j = num; j <= num3; j++) {
					ushort num5 = Singleton<VehicleManager>.instance.m_vehicleGrid [i * 540 + j];
					if (num5 != 0) {
						this.m_renderBuffer [num5 >> 6] |= 1uL << (int)num5;
					}
				}
			}
			float near2 = cameraInfo.m_near;
			float d2 = Mathf.Min (2000f, cameraInfo.m_far);
			Vector3 lhs5 = cameraInfo.m_position + cameraInfo.m_directionA * near2;
			Vector3 rhs5 = cameraInfo.m_position + cameraInfo.m_directionB * near2;
			Vector3 lhs6 = cameraInfo.m_position + cameraInfo.m_directionC * near2;
			Vector3 rhs6 = cameraInfo.m_position + cameraInfo.m_directionD * near2;
			Vector3 lhs7 = cameraInfo.m_position + cameraInfo.m_directionA * d2;
			Vector3 rhs7 = cameraInfo.m_position + cameraInfo.m_directionB * d2;
			Vector3 lhs8 = cameraInfo.m_position + cameraInfo.m_directionC * d2;
			Vector3 rhs8 = cameraInfo.m_position + cameraInfo.m_directionD * d2;
			Vector3 vector3 = Vector3.Min (Vector3.Min (Vector3.Min (lhs5, rhs5), Vector3.Min (lhs6, rhs6)), Vector3.Min (Vector3.Min (lhs7, rhs7), Vector3.Min (lhs8, rhs8)));
			Vector3 vector4 = Vector3.Max (Vector3.Max (Vector3.Max (lhs5, rhs5), Vector3.Max (lhs6, rhs6)), Vector3.Max (Vector3.Max (lhs7, rhs7), Vector3.Max (lhs8, rhs8)));
			int num6 = Mathf.Max ((int)((vector3.x - 10f) / 32f + 270f), 0);
			int num7 = Mathf.Max ((int)((vector3.z - 10f) / 32f + 270f), 0);
			int num8 = Mathf.Min ((int)((vector4.x + 10f) / 32f + 270f), 539);
			int num9 = Mathf.Min ((int)((vector4.z + 10f) / 32f + 270f), 539);
			for (int k = num7; k <= num9; k++) {
				for (int l = num6; l <= num8; l++) {
					ushort num10 = Singleton<VehicleManager>.instance.m_parkedGrid [k * 540 + l];
					if (num10 != 0) {
						this.m_renderBuffer2 [num10 >> 6] |= 1uL << (int)num10;
					}
				}
			}
			float near3 = cameraInfo.m_near;
			float d3 = Mathf.Min (10000f, cameraInfo.m_far);
			Vector3 lhs9 = cameraInfo.m_position + cameraInfo.m_directionA * near3;
			Vector3 rhs9 = cameraInfo.m_position + cameraInfo.m_directionB * near3;
			Vector3 lhs10 = cameraInfo.m_position + cameraInfo.m_directionC * near3;
			Vector3 rhs10 = cameraInfo.m_position + cameraInfo.m_directionD * near3;
			Vector3 lhs11 = cameraInfo.m_position + cameraInfo.m_directionA * d3;
			Vector3 rhs11 = cameraInfo.m_position + cameraInfo.m_directionB * d3;
			Vector3 lhs12 = cameraInfo.m_position + cameraInfo.m_directionC * d3;
			Vector3 rhs12 = cameraInfo.m_position + cameraInfo.m_directionD * d3;
			Vector3 vector5 = Vector3.Min (Vector3.Min (Vector3.Min (lhs9, rhs9), Vector3.Min (lhs10, rhs10)), Vector3.Min (Vector3.Min (lhs11, rhs11), Vector3.Min (lhs12, rhs12)));
			Vector3 vector6 = Vector3.Max (Vector3.Max (Vector3.Max (lhs9, rhs9), Vector3.Max (lhs10, rhs10)), Vector3.Max (Vector3.Max (lhs11, rhs11), Vector3.Max (lhs12, rhs12)));
			int num11 = Mathf.Max ((int)((vector5.x - 50f) / 320f + 27f), 0);
			int num12 = Mathf.Max ((int)((vector5.z - 50f) / 320f + 27f), 0);
			int num13 = Mathf.Min ((int)((vector6.x + 50f) / 320f + 27f), 53);
			int num14 = Mathf.Min ((int)((vector6.z + 50f) / 320f + 27f), 53);
			for (int m = num12; m <= num14; m++) {
				for (int n = num11; n <= num13; n++) {
					ushort num15 = Singleton<VehicleManager>.instance.m_vehicleGrid2 [m * 54 + n];
					if (num15 != 0) {
						this.m_renderBuffer [num15 >> 6] |= 1uL << (int)num15;
					}
				}
			}
			int num16 = this.m_renderBuffer.Length;
			for (int num17 = 0; num17 < num16; num17++) {
				ulong num18 = this.m_renderBuffer [num17];
				if (num18 != 0uL) {
					for (int num19 = 0; num19 < 64; num19++) {
						ulong num20 = 1uL << num19;
						if ((num18 & num20) != 0uL) {
							ushort num21 = (ushort)(num17 << 6 | num19);
							if (!Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)num21].RenderInstance (cameraInfo, num21)) {
								num18 &= ~num20;
							}
							ushort nextGridVehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)num21].m_nextGridVehicle;
							int num22 = 0;
							while (nextGridVehicle != 0) {
								int num23 = nextGridVehicle >> 6;
								num20 = 1uL << (int)nextGridVehicle;
								if (num23 == num17) {
									if ((num18 & num20) != 0uL) {
										break;
									}
									num18 |= num20;
								} else {
									ulong num24 = this.m_renderBuffer [num23];
									if ((num24 & num20) != 0uL) {
										break;
									}
									this.m_renderBuffer [num23] = (num24 | num20);
								}
								if (nextGridVehicle > num21) {
									break;
								}
								nextGridVehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)nextGridVehicle].m_nextGridVehicle;
								if (++num22 > 65536) {
									CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
									break;
								}
							}
						}
					}
					this.m_renderBuffer [num17] = num18;
				}
			}
			int num25 = this.m_renderBuffer2.Length;
			for (int num26 = 0; num26 < num25; num26++) {
				ulong num27 = this.m_renderBuffer2 [num26];
				if (num27 != 0uL) {
					for (int num28 = 0; num28 < 64; num28++) {
						ulong num29 = 1uL << num28;
						if ((num27 & num29) != 0uL) {
							ushort num30 = (ushort)(num26 << 6 | num28);
							if (!Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)num30].RenderInstance (cameraInfo, num30)) {
								num27 &= ~num29;
							}
							ushort nextGridParked = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)num30].m_nextGridParked;
							int num31 = 0;
							while (nextGridParked != 0) {
								int num32 = nextGridParked >> 6;
								num29 = 1uL << (int)nextGridParked;
								if (num32 == num26) {
									if ((num27 & num29) != 0uL) {
										break;
									}
									num27 |= num29;
								} else {
									ulong num33 = this.m_renderBuffer2 [num32];
									if ((num33 & num29) != 0uL) {
										break;
									}
									this.m_renderBuffer2 [num32] = (num33 | num29);
								}
								if (nextGridParked > num30) {
									break;
								}
								nextGridParked = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)nextGridParked].m_nextGridParked;
								if (++num31 > 32768) {
									CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
									break;
								}
							}
						}
					}
					this.m_renderBuffer2 [num26] = num27;
				}
			}
			int num34 = PrefabCollection<VehicleInfo>.PrefabCount ();
			for (int num35 = 0; num35 < num34; num35++) {
				VehicleInfo prefab = PrefabCollection<VehicleInfo>.GetPrefab ((uint)num35);
				if (prefab != null) {
					if (prefab.m_lodCount != 0) {
						Vehicle.RenderLod (cameraInfo, prefab);
					}
					if (prefab.m_undergroundLodCount != 0) {
						Vehicle.RenderUndergroundLod (cameraInfo, prefab);
					}
					if (prefab.m_subMeshes != null) {
						for (int num36 = 0; num36 < prefab.m_subMeshes.Length; num36++) {
							VehicleInfoBase subInfo = prefab.m_subMeshes [num36].m_subInfo;
							if (subInfo != null) {
								if (subInfo.m_lodCount != 0) {
									Vehicle.RenderLod (cameraInfo, subInfo);
								}
								if (subInfo.m_undergroundLodCount != 0) {
									Vehicle.RenderUndergroundLod (cameraInfo, subInfo);
								}
							}
						}
					}
				}
			}
		}
	
		private string GenerateParkedVehicleName (ushort parkedID)
		{
			VehicleInfo info = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)parkedID].Info;
			if (info != null) {
				string key = PrefabCollection<VehicleInfo>.PrefabName ((uint)info.m_prefabDataIndex);
				return Locale.Get ("VEHICLE_TITLE", key);
			}
			return "Invalid";
		}
	
		private string GenerateVehicleName (ushort vehicleID)
		{
			VehicleInfo info = Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicleID].Info;
			if (info != null) {
				string key = PrefabCollection<VehicleInfo>.PrefabName ((uint)info.m_prefabDataIndex);
				return Locale.Get ("VEHICLE_TITLE", key);
			}
			return "Invalid";
		}
	
		public void GetData (FastList<IDataContainer> data)
		{
			data.Add (new FakeVehicleManager.Data ());
		}
	
		public string GetDefaultParkedVehicleName (ushort parkedID)
		{
			return this.GenerateParkedVehicleName (parkedID);
		}
	
		public string GetDefaultVehicleName (ushort vehicleID)
		{
			return this.GenerateVehicleName (vehicleID);
		}
	
		public string GetParkedVehicleName (ushort parkedID)
		{
			if (Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)parkedID].m_flags != 0) {
				string text = null;
				if ((Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)parkedID].m_flags & 16) != 0) {
					InstanceID id = default(InstanceID);
					id.ParkedVehicle = parkedID;
					text = Singleton<InstanceManager>.instance.GetName (id);
				}
				if (text == null) {
					text = this.GenerateParkedVehicleName (parkedID);
				}
				return text;
			}
			return null;
		}
	
		public VehicleInfo GetRandomVehicleInfo (ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level)
		{
			if (!this.m_vehiclesRefreshed) {
				CODebugBase<LogChannel>.Error (LogChannel.Core, "Random vehicles not refreshed yet!");
				return null;
			}
			int num = FakeVehicleManager.GetTransferIndex (service, subService, level);
			FastList<ushort> fastList = this.m_transferVehicles [num];
			if (fastList == null) {
				return null;
			}
			if (fastList.m_size == 0) {
				return null;
			}
			num = r.Int32 ((uint)fastList.m_size);
			return PrefabCollection<VehicleInfo>.GetPrefab ((uint)fastList.m_buffer [num]);
		}
	
		public string GetVehicleName (ushort vehicleID)
		{
			if (Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicleID].m_flags != Vehicle.Flags.None) {
				string text = null;
				if ((Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicleID].m_flags & Vehicle.Flags.CustomName) != Vehicle.Flags.None) {
					InstanceID id = default(InstanceID);
					id.Vehicle = vehicleID;
					text = Singleton<InstanceManager>.instance.GetName (id);
				}
				if (text == null) {
					text = this.GenerateVehicleName (vehicleID);
				}
				return text;
			}
			return null;
		}
	
		[DebuggerHidden]
		private IEnumerator InitRenderDataImpl ()
		{
			FakeVehicleManager.RendererIterator RendererIterator__IteratorB = new FakeVehicleManager.RendererIterator ();
			RendererIterator__IteratorB.f__this = this;
			return RendererIterator__IteratorB;
		}
	
		private void OnDestroy ()
		{
			if (Singleton<VehicleManager>.instance.m_lodRgbAtlas != null) {
				UnityEngine.Object.Destroy (Singleton<VehicleManager>.instance.m_lodRgbAtlas);
				Singleton<VehicleManager>.instance.m_lodRgbAtlas = null;
			}
			if (Singleton<VehicleManager>.instance.m_lodXysAtlas != null) {
				UnityEngine.Object.Destroy (Singleton<VehicleManager>.instance.m_lodXysAtlas);
				Singleton<VehicleManager>.instance.m_lodXysAtlas = null;
			}
			if (Singleton<VehicleManager>.instance.m_lodAciAtlas != null) {
				UnityEngine.Object.Destroy (Singleton<VehicleManager>.instance.m_lodAciAtlas);
				Singleton<VehicleManager>.instance.m_lodAciAtlas = null;
			}
		}
	
		public bool RayCast (Segment3 ray, Vehicle.Flags ignoreFlags, VehicleParked.Flags ignoreFlags2, out Vector3 hit, out ushort vehicleIndex, out ushort parkedIndex)
		{
			hit = ray.b;
			vehicleIndex = 0;
			parkedIndex = 0;
			Bounds bounds = new Bounds (new Vector3 (0f, 512f, 0f), new Vector3 (17280f, 1152f, 17280f));
			Segment3 ray2 = ray;
			if (ray2.Clip (bounds)) {
				Vector3 vector = ray2.b - ray2.a;
				Vector3 normalized = vector.normalized;
				Vector3 vector2 = ray2.a - normalized * 72f;
				Vector3 vector3 = ray2.a + Vector3.ClampMagnitude (ray2.b - ray2.a, 2000f) + normalized * 72f;
				int num = (int)(vector2.x / 32f + 270f);
				int num2 = (int)(vector2.z / 32f + 270f);
				int num3 = (int)(vector3.x / 32f + 270f);
				int num4 = (int)(vector3.z / 32f + 270f);
				float num5 = Mathf.Abs (vector.x);
				float num6 = Mathf.Abs (vector.z);
				int num7;
				int num8;
				if (num5 >= num6) {
					num7 = ((vector.x <= 0f) ? -1 : 1);
					num8 = 0;
					if (num5 != 0f) {
						vector *= 32f / num5;
					}
				} else {
					num7 = 0;
					num8 = ((vector.z <= 0f) ? -1 : 1);
					if (num6 != 0f) {
						vector *= 32f / num6;
					}
				}
				Vector3 vector4 = vector2;
				Vector3 vector5 = vector2;
				do {
					Vector3 vector6 = vector5 + vector;
					int num9;
					int num10;
					int num11;
					int num12;
					if (num7 != 0) {
						num9 = Mathf.Max (num, 0);
						num10 = Mathf.Min (num, 539);
						num11 = Mathf.Max ((int)((Mathf.Min (vector4.z, vector6.z) - 72f) / 32f + 270f), 0);
						num12 = Mathf.Min ((int)((Mathf.Max (vector4.z, vector6.z) + 72f) / 32f + 270f), 539);
					} else {
						num11 = Mathf.Max (num2, 0);
						num12 = Mathf.Min (num2, 539);
						num9 = Mathf.Max ((int)((Mathf.Min (vector4.x, vector6.x) - 72f) / 32f + 270f), 0);
						num10 = Mathf.Min ((int)((Mathf.Max (vector4.x, vector6.x) + 72f) / 32f + 270f), 539);
					}
					for (int i = num11; i <= num12; i++) {
						for (int j = num9; j <= num10; j++) {
							ushort num13 = Singleton<VehicleManager>.instance.m_vehicleGrid [i * 540 + j];
							int num14 = 0;
							while (num13 != 0) {
								float t;
								if (Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)num13].RayCast (num13, ray2, ignoreFlags, out t)) {
									Vector3 vector7 = ray2.Position (t);
									if (Vector3.SqrMagnitude (vector7 - ray.a) < Vector3.SqrMagnitude (hit - ray.a)) {
										hit = vector7;
										vehicleIndex = num13;
										parkedIndex = 0;
									}
								}
								num13 = Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)num13].m_nextGridVehicle;
								if (++num14 > 65536) {
									CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
									break;
								}
							}
							ushort num15 = Singleton<VehicleManager>.instance.m_parkedGrid [i * 540 + j];
							int num16 = 0;
							while (num15 != 0) {
								float t2;
								if (Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)num15].RayCast (num15, ray2, ignoreFlags2, out t2)) {
									Vector3 vector8 = ray2.Position (t2);
									if (Vector3.SqrMagnitude (vector8 - ray.a) < Vector3.SqrMagnitude (hit - ray.a)) {
										hit = vector8;
										vehicleIndex = 0;
										parkedIndex = num15;
									}
								}
								num15 = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)num15].m_nextGridParked;
								if (++num16 > 32768) {
									CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
									break;
								}
							}
						}
					}
					vector4 = vector5;
					vector5 = vector6;
					num += num7;
					num2 += num8;
				} while ((num <= num3 || num7 <= 0) && (num >= num3 || num7 >= 0) && (num2 <= num4 || num8 <= 0) && (num2 >= num4 || num8 >= 0));
			}
			bounds = new Bounds (new Vector3 (0f, 1512f, 0f), new Vector3 (17280f, 3024f, 17280f));
			ray2 = ray;
			if (ray2.Clip (bounds)) {
				Vector3 vector9 = ray2.b - ray2.a;
				Vector3 normalized2 = vector9.normalized;
				Vector3 vector10 = ray2.a - normalized2 * 112f;
				Vector3 vector11 = ray2.b + normalized2 * 112f;
				int num17 = (int)(vector10.x / 320f + 27f);
				int num18 = (int)(vector10.z / 320f + 27f);
				int num19 = (int)(vector11.x / 320f + 27f);
				int num20 = (int)(vector11.z / 320f + 27f);
				float num21 = Mathf.Abs (vector9.x);
				float num22 = Mathf.Abs (vector9.z);
				int num23;
				int num24;
				if (num21 >= num22) {
					num23 = ((vector9.x <= 0f) ? -1 : 1);
					num24 = 0;
					if (num21 != 0f) {
						vector9 *= 320f / num21;
					}
				} else {
					num23 = 0;
					num24 = ((vector9.z <= 0f) ? -1 : 1);
					if (num22 != 0f) {
						vector9 *= 320f / num22;
					}
				}
				Vector3 vector12 = vector10;
				Vector3 vector13 = vector10;
				do {
					Vector3 vector14 = vector13 + vector9;
					int num25;
					int num26;
					int num27;
					int num28;
					if (num23 != 0) {
						num25 = Mathf.Max (num17, 0);
						num26 = Mathf.Min (num17, 53);
						num27 = Mathf.Max ((int)((Mathf.Min (vector12.z, vector14.z) - 112f) / 320f + 27f), 0);
						num28 = Mathf.Min ((int)((Mathf.Max (vector12.z, vector14.z) + 112f) / 320f + 27f), 53);
					} else {
						num27 = Mathf.Max (num18, 0);
						num28 = Mathf.Min (num18, 53);
						num25 = Mathf.Max ((int)((Mathf.Min (vector12.x, vector14.x) - 112f) / 320f + 27f), 0);
						num26 = Mathf.Min ((int)((Mathf.Max (vector12.x, vector14.x) + 112f) / 320f + 27f), 53);
					}
					for (int k = num27; k <= num28; k++) {
						for (int l = num25; l <= num26; l++) {
							ushort num29 = Singleton<VehicleManager>.instance.m_vehicleGrid2 [k * 54 + l];
							int num30 = 0;
							while (num29 != 0) {
								float t3;
								if (Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)num29].RayCast (num29, ray2, ignoreFlags, out t3)) {
									Vector3 vector15 = ray2.Position (t3);
									if (Vector3.SqrMagnitude (vector15 - ray.a) < Vector3.SqrMagnitude (hit - ray.a)) {
										hit = vector15;
										vehicleIndex = num29;
										parkedIndex = 0;
									}
								}
								num29 = Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)num29].m_nextGridVehicle;
								if (++num30 > 65536) {
									CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
									break;
								}
							}
						}
					}
					vector12 = vector13;
					vector13 = vector14;
					num17 += num23;
					num18 += num24;
				} while ((num17 <= num19 || num23 <= 0) && (num17 >= num19 || num23 >= 0) && (num18 <= num20 || num24 <= 0) && (num18 >= num20 || num24 >= 0));
			}
			if (vehicleIndex != 0 || parkedIndex != 0) {
				return true;
			}
			hit = Vector3.zero;
			vehicleIndex = 0;
			parkedIndex = 0;
			return false;
		}
	
		private void RefreshTransferVehicles ()
		{
			int num = this.m_transferVehicles.Length;
			for (int i = 0; i < num; i++) {
				this.m_transferVehicles [i] = null;
			}
			int num2 = PrefabCollection<VehicleInfo>.PrefabCount ();
			for (int j = 0; j < num2; j++) {
				VehicleInfo prefab = PrefabCollection<VehicleInfo>.GetPrefab ((uint)j);
				if (prefab != null && prefab.m_class.m_service != ItemClass.Service.None && prefab.m_placementStyle == ItemClass.Placement.Automatic) {
					int transferIndex = FakeVehicleManager.GetTransferIndex (prefab.m_class.m_service, prefab.m_class.m_subService, prefab.m_class.m_level);
					if (this.m_transferVehicles [transferIndex] == null) {
						this.m_transferVehicles [transferIndex] = new FastList<ushort> ();
					}
					this.m_transferVehicles [transferIndex].Add ((ushort)j);
				}
			}
			int num3 = 39;
			for (int k = 0; k < num3; k++) {
				for (int l = 1; l < 5; l++) {
					int num4 = k;
					num4 = num4 * 5 + l;
					FastList<ushort> fastList = this.m_transferVehicles [num4];
					FastList<ushort> fastList2 = this.m_transferVehicles [num4 - 1];
					if (fastList == null && fastList2 != null) {
						this.m_transferVehicles [num4] = fastList2;
					}
				}
			}
			this.m_vehiclesRefreshed = true;
		}
	
		public void ReleaseParkedVehicle (ushort parked)
		{
			this.ReleaseParkedVehicleImplementation (parked, ref Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)parked]);
		}
	
		private void ReleaseParkedVehicleImplementation (ushort parked, ref VehicleParked data)
		{
			if (data.m_flags != 0) {
				InstanceID id = default(InstanceID);
				id.ParkedVehicle = parked;
				Singleton<InstanceManager>.instance.ReleaseInstance (id);
				data.m_flags |= 2;
				this.RemoveFromGrid (parked, ref data);
				if (data.m_ownerCitizen != 0u) {
					Singleton<CitizenManager>.instance.m_citizens.m_buffer [(int)((UIntPtr)data.m_ownerCitizen)].m_parkedVehicle = 0;
					data.m_ownerCitizen = 0u;
				}
				data.m_flags = 0;
				Singleton<VehicleManager>.instance.m_parkedVehicles.ReleaseItem (parked);
				Singleton<VehicleManager>.instance.m_parkedCount = (int)(Singleton<VehicleManager>.instance.m_parkedVehicles.ItemCount () - 1u);
			}
		}
	
		public void ReleaseVehicle (ushort vehicle)
		{
			this.ReleaseVehicleImplementation (vehicle, ref Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)vehicle]);
		}
	
		private void ReleaseVehicleImplementation (ushort vehicle, ref Vehicle data)
		{
			if (data.m_flags != Vehicle.Flags.None) {
				InstanceID id = default(InstanceID);
				id.Vehicle = vehicle;
				Singleton<InstanceManager>.instance.ReleaseInstance (id);
				data.m_flags |= Vehicle.Flags.Deleted;
				data.Unspawn (vehicle);
				VehicleInfo info = data.Info;
				if (info != null) {
					info.m_vehicleAI.ReleaseVehicle (vehicle, ref data);
				}
				if (data.m_leadingVehicle != 0) {
					if (Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)data.m_leadingVehicle].m_trailingVehicle == vehicle) {
						Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)data.m_leadingVehicle].m_trailingVehicle = 0;
					}
					data.m_leadingVehicle = 0;
				}
				if (data.m_trailingVehicle != 0) {
					if (Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)data.m_trailingVehicle].m_leadingVehicle == vehicle) {
						Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)data.m_trailingVehicle].m_leadingVehicle = 0;
					}
					data.m_trailingVehicle = 0;
				}
				if (data.m_cargoParent != 0) {
					ushort num = 0;
					ushort num2 = Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)data.m_cargoParent].m_firstCargo;
					int num3 = 0;
					while (num2 != 0) {
						if (num2 == vehicle) {
							if (num == 0) {
								Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)data.m_cargoParent].m_firstCargo = data.m_nextCargo;
							} else {
								Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)num].m_nextCargo = data.m_nextCargo;
							}
							break;
						}
						num = num2;
						num2 = Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)num2].m_nextCargo;
						if (++num3 > 65536) {
							CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
							break;
						}
					}
					data.m_cargoParent = 0;
					data.m_nextCargo = 0;
				}
				if (data.m_firstCargo != 0) {
					ushort num4 = data.m_firstCargo;
					int num5 = 0;
					while (num4 != 0) {
						ushort nextCargo = Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)num4].m_nextCargo;
						Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)num4].m_cargoParent = 0;
						Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)num4].m_nextCargo = 0;
						num4 = nextCargo;
						if (++num5 > 65536) {
							CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
							break;
						}
					}
					data.m_firstCargo = 0;
				}
				if (data.m_path != 0u) {
					Singleton<PathManager>.instance.ReleasePath (data.m_path);
					data.m_path = 0u;
				}
				if (data.m_citizenUnits != 0u) {
					Singleton<CitizenManager>.instance.ReleaseUnits (data.m_citizenUnits);
					data.m_citizenUnits = 0u;
				}
				data.m_flags = Vehicle.Flags.None;
				Singleton<VehicleManager>.instance.m_vehicles.ReleaseItem (vehicle);
				Singleton<VehicleManager>.instance.m_vehicleCount = (int)(Singleton<VehicleManager>.instance.m_vehicles.ItemCount () - 1u);
			}
		}
	
		public void RemoveFromGrid (ushort vehicle, ref Vehicle data, bool large, int gridX, int gridZ)
		{
			if (large) {
				int num = gridZ * 54 + gridX;
				ushort num2 = 0;
				ushort num3 = Singleton<VehicleManager>.instance.m_vehicleGrid2 [num];
				int num4 = 0;
				while (num3 != 0) {
					if (num3 == vehicle) {
						if (num2 == 0) {
							Singleton<VehicleManager>.instance.m_vehicleGrid2 [num] = data.m_nextGridVehicle;
						} else {
							Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)num2].m_nextGridVehicle = data.m_nextGridVehicle;
						}
						break;
					}
					num2 = num3;
					num3 = Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)num3].m_nextGridVehicle;
					if (++num4 > 65536) {
						CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
						break;
					}
				}
				data.m_nextGridVehicle = 0;
			} else {
				int num5 = gridZ * 540 + gridX;
				ushort num6 = 0;
				ushort num7 = Singleton<VehicleManager>.instance.m_vehicleGrid [num5];
				int num8 = 0;
				while (num7 != 0) {
					if (num7 == vehicle) {
						if (num6 == 0) {
							Singleton<VehicleManager>.instance.m_vehicleGrid [num5] = data.m_nextGridVehicle;
						} else {
							Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)num6].m_nextGridVehicle = data.m_nextGridVehicle;
						}
						break;
					}
					num6 = num7;
					num7 = Singleton<VehicleManager>.instance.m_vehicles.m_buffer [(int)num7].m_nextGridVehicle;
					if (++num8 > 65536) {
						CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
						break;
					}
				}
				data.m_nextGridVehicle = 0;
			}
		}
	
		public void RemoveFromGrid (ushort vehicle, ref Vehicle data, bool large)
		{
			Vector3 lastFramePosition = data.GetLastFramePosition ();
			if (large) {
				int gridX = Mathf.Clamp ((int)(lastFramePosition.x / 320f + 27f), 0, 53);
				int gridZ = Mathf.Clamp ((int)(lastFramePosition.z / 320f + 27f), 0, 53);
				this.RemoveFromGrid (vehicle, ref data, large, gridX, gridZ);
			} else {
				int gridX2 = Mathf.Clamp ((int)(lastFramePosition.x / 32f + 270f), 0, 539);
				int gridZ2 = Mathf.Clamp ((int)(lastFramePosition.z / 32f + 270f), 0, 539);
				this.RemoveFromGrid (vehicle, ref data, large, gridX2, gridZ2);
			}
		}
	
		public void RemoveFromGrid (ushort parked, ref VehicleParked data, int gridX, int gridZ)
		{
			int num = gridZ * 540 + gridX;
			ushort num2 = 0;
			ushort num3 = Singleton<VehicleManager>.instance.m_parkedGrid [num];
			int num4 = 0;
			while (num3 != 0) {
				if (num3 == parked) {
					if (num2 == 0) {
						Singleton<VehicleManager>.instance.m_parkedGrid [num] = data.m_nextGridParked;
					} else {
						Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)num2].m_nextGridParked = data.m_nextGridParked;
					}
					break;
				}
				num2 = num3;
				num3 = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)num3].m_nextGridParked;
				if (++num4 > 32768) {
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			data.m_nextGridParked = 0;
		}
	
		public void RemoveFromGrid (ushort parked, ref VehicleParked data)
		{
			int gridX = Mathf.Clamp ((int)(data.m_position.x / 32f + 270f), 0, 539);
			int gridZ = Mathf.Clamp ((int)(data.m_position.z / 32f + 270f), 0, 539);
			this.RemoveFromGrid (parked, ref data, gridX, gridZ);
		}
	
		[DebuggerHidden]
		public bool SetParkedVehicleName (ushort parkedID, string name)
		{
			bool result = false;
			VehicleParked.Flags flag = (VehicleParked.Flags)Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)parkedID].m_flags;
			if (parkedID != 0 && flag != VehicleParked.Flags.None) {
				if (!name.IsNullOrWhiteSpace () && name != this.GenerateParkedVehicleName (parkedID)) {
					Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)parkedID].m_flags = (ushort)(flag | VehicleParked.Flags.CustomName);
					InstanceID instance = default(InstanceID);
					instance.ParkedVehicle = parkedID;
					Singleton<InstanceManager>.instance.SetName (instance, name);
					Singleton<GuideManager>.instance.m_renameNotUsed.Disable ();
				} else {
					if ((flag & VehicleParked.Flags.CustomName) != VehicleParked.Flags.None) {
						Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)parkedID].m_flags = (ushort)(flag & ~VehicleParked.Flags.CustomName);
						InstanceID instance = default(InstanceID);
						instance.ParkedVehicle = parkedID;
						Singleton<InstanceManager>.instance.SetName (instance, null);
					}
				}
				result = true;
			}
			return result;
		}
	
//	[DebuggerHidden]
//	public IEnumerator<bool> SetVehicleName (ushort vehicleID, string name)
//	{
//		FakeVehicleManager.<SetVehicleName>c__IteratorB1 <SetVehicleName>c__IteratorB = new FakeVehicleManager.<SetVehicleName>c__IteratorB1 ();
//		<SetVehicleName>c__IteratorB.vehicleID = vehicleID;
//		<SetVehicleName>c__IteratorB.name = name;
//		<SetVehicleName>c__IteratorB.<$>vehicleID = vehicleID;
//		<SetVehicleName>c__IteratorB.<$>name = name;
//		<SetVehicleName>c__IteratorB.f__this = this;
//		return <SetVehicleName>c__IteratorB;
//	}
	

	
		protected void SimulationStepImpl (int subStep)
		{
			if (Singleton<VehicleManager>.instance.m_parkedUpdated) {
				int num = Singleton<VehicleManager>.instance.m_updatedParked.Length;
				for (int i = 0; i < num; i++) {
					ulong num2 = Singleton<VehicleManager>.instance.m_updatedParked [i];
					if (num2 != 0uL) {
						Singleton<VehicleManager>.instance.m_updatedParked [i] = 0uL;
						for (int j = 0; j < 64; j++) {
							if ((num2 & 1uL << j) != 0uL) {
								ushort num3 = (ushort)(i << 6 | j);
								VehicleInfo info = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)num3].Info;
								VehicleParked[] expr_7C_cp_0 = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer;
								ushort expr_7C_cp_1 = num3;
								expr_7C_cp_0 [(int)expr_7C_cp_1].m_flags = (ushort)(expr_7C_cp_0 [(int)expr_7C_cp_1].m_flags & 65531);
								info.m_vehicleAI.UpdateParkedVehicle (num3, ref Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)num3]);
							}
						}
					}
				}
				Singleton<VehicleManager>.instance.m_parkedUpdated = false;
			}
			if (subStep != 0) {
				SimulationManager instance = Singleton<SimulationManager>.instance;
				Vector3 physicsLodRefPos = instance.m_simulationView.m_position + instance.m_simulationView.m_direction * 1000f;
				int num4 = (int)(instance.m_currentFrameIndex & 15u);
				int num5 = num4 * 1024;
				int num6 = (num4 + 1) * 1024 - 1;
				for (int k = num5; k <= num6; k++) {
					Vehicle.Flags flags = Singleton<VehicleManager>.instance.m_vehicles.m_buffer [k].m_flags;
					if ((flags & Vehicle.Flags.Created) != Vehicle.Flags.None && Singleton<VehicleManager>.instance.m_vehicles.m_buffer [k].m_leadingVehicle == 0) {
						VehicleInfo info2 = Singleton<VehicleManager>.instance.m_vehicles.m_buffer [k].Info;
						info2.m_vehicleAI.SimulationStep ((ushort)k, ref Singleton<VehicleManager>.instance.m_vehicles.m_buffer [k], physicsLodRefPos);
					}
				}
			}
		}
	
		public void UpdateData (SimulationManager.UpdateMode mode)
		{
			Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.BeginLoading ("FakeVehicleManager.UpdateData");
			for (int i = 1; i < 65536; i++) {
				if (Singleton<VehicleManager>.instance.m_vehicles.m_buffer [i].m_flags != Vehicle.Flags.None && Singleton<VehicleManager>.instance.m_vehicles.m_buffer [i].Info == null) {
					this.ReleaseVehicle ((ushort)i);
				}
			}
			for (int j = 1; j < 32768; j++) {
				if (Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [j].m_flags != 0 && Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [j].Info == null) {
					this.ReleaseParkedVehicle ((ushort)j);
				}
			}
			Singleton<VehicleManager>.instance.m_infoCount = PrefabCollection<VehicleInfo>.PrefabCount ();
			Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.EndLoading ();
		}
	
		public void UpdateParkedVehicles (float minX, float minZ, float maxX, float maxZ)
		{
			int num = Mathf.Max ((int)((minX - 10f) / 32f + 270f), 0);
			int num2 = Mathf.Max ((int)((minZ - 10f) / 32f + 270f), 0);
			int num3 = Mathf.Min ((int)((maxX + 10f) / 32f + 270f), 539);
			int num4 = Mathf.Min ((int)((maxZ + 10f) / 32f + 270f), 539);
			for (int i = num2; i <= num4; i++) {
				for (int j = num; j <= num3; j++) {
					ushort num5 = Singleton<VehicleManager>.instance.m_parkedGrid [i * 540 + j];
					int num6 = 0;
					while (num5 != 0) {
						if ((Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)num5].m_flags & 4) == 0) {
							VehicleParked[] expr_D1_cp_0 = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer;
							ushort expr_D1_cp_1 = num5;
							expr_D1_cp_0 [(int)expr_D1_cp_1].m_flags = (ushort)(expr_D1_cp_0 [(int)expr_D1_cp_1].m_flags | 4);
							Singleton<VehicleManager>.instance.m_updatedParked [num5 >> 6] |= 1uL << (int)num5;
							Singleton<VehicleManager>.instance.m_parkedUpdated = true;
						}
						num5 = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer [(int)num5].m_nextGridParked;
						if (++num6 > 32768) {
							CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
							break;
						}
					}
				}
			}
		}
	
		//
		// Nested Types
		//
		public class Data : IDataContainer
		{
			public void Serialize (DataSerializer s)
			{
				Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.BeginSerialize (s, "FakeVehicleManager");
				FakeVehicleManager instance = FakeVehicleManager.instance;
				Vehicle[] buffer = instance.m_vehicles.m_buffer;
				VehicleParked[] buffer2 = instance.m_parkedVehicles.m_buffer;
				int num = buffer.Length;
				int num2 = buffer2.Length;
				EncodedArray.UInt uInt = EncodedArray.UInt.BeginWrite (s);
				for (int i = 1; i < num; i++) {
					uInt.Write ((uint)buffer [i].m_flags);
				}
				uInt.EndWrite ();
				EncodedArray.UShort uShort = EncodedArray.UShort.BeginWrite (s);
				for (int j = 1; j < num2; j++) {
					uShort.Write (buffer2 [j].m_flags);
				}
				uShort.EndWrite ();
				try {
					PrefabCollection<VehicleInfo>.BeginSerialize (s);
					for (int k = 1; k < num; k++) {
						if (buffer [k].m_flags != Vehicle.Flags.None) {
							PrefabCollection<VehicleInfo>.Serialize ((uint)buffer [k].m_infoIndex);
						}
					}
					for (int l = 1; l < num2; l++) {
						if (buffer2 [l].m_flags != 0) {
							PrefabCollection<VehicleInfo>.Serialize ((uint)buffer2 [l].m_infoIndex);
						}
					}
				} finally {
					PrefabCollection<VehicleInfo>.EndSerialize (s);
				}
				EncodedArray.Byte @byte = EncodedArray.Byte.BeginWrite (s);
				for (int m = 1; m < num; m++) {
					if (buffer [m].m_flags != Vehicle.Flags.None) {
							@byte.Write (buffer [m].m_gateIndex);
					}
				}
					@byte.EndWrite ();
				for (int n = 1; n < num; n++) {
					if (buffer [n].m_flags != Vehicle.Flags.None) {
						Vehicle.Frame lastFrameData = buffer [n].GetLastFrameData ();
						s.WriteVector3 (lastFrameData.m_velocity);
						s.WriteVector3 (lastFrameData.m_position);
						s.WriteQuaternion (lastFrameData.m_rotation);
						s.WriteFloat (lastFrameData.m_angleVelocity);
						s.WriteVector4 (buffer [n].m_targetPos0);
						s.WriteVector4 (buffer [n].m_targetPos1);
						s.WriteVector4 (buffer [n].m_targetPos2);
						s.WriteVector4 (buffer [n].m_targetPos3);
						s.WriteUInt16 ((uint)buffer [n].m_sourceBuilding);
						s.WriteUInt16 ((uint)buffer [n].m_targetBuilding);
						s.WriteUInt16 ((uint)buffer [n].m_transportLine);
						s.WriteUInt16 ((uint)buffer [n].m_transferSize);
						s.WriteUInt8 ((uint)buffer [n].m_transferType);
						s.WriteUInt8 ((uint)buffer [n].m_waitCounter);
						s.WriteUInt8 ((uint)buffer [n].m_blockCounter);
						s.WriteUInt24 (buffer [n].m_citizenUnits);
						s.WriteUInt24 (buffer [n].m_path);
						s.WriteUInt8 ((uint)buffer [n].m_pathPositionIndex);
						s.WriteUInt8 ((uint)buffer [n].m_lastPathOffset);
						s.WriteUInt16 ((uint)buffer [n].m_trailingVehicle);
						s.WriteUInt16 ((uint)buffer [n].m_cargoParent);
					}
				}
				for (int num3 = 1; num3 < num2; num3++) {
					if (buffer2 [num3].m_flags != 0) {
						s.WriteVector3 (buffer2 [num3].m_position);
						s.WriteQuaternion (buffer2 [num3].m_rotation);
						s.WriteUInt24 (buffer2 [num3].m_ownerCitizen);
					}
				}
				Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.EndSerialize (s, "FakeVehicleManager");
			}

			public void Deserialize (DataSerializer s)
			{
				Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.BeginDeserialize (s, "FakeVehicleManager");
				FakeVehicleManager instance = FakeVehicleManager.instance;
				Vehicle[] buffer = instance.m_vehicles.m_buffer;
				VehicleParked[] buffer2 = instance.m_parkedVehicles.m_buffer;
				ushort[] vehicleGrid = instance.m_vehicleGrid;
				ushort[] vehicleGrid2 = instance.m_vehicleGrid2;
				ushort[] parkedGrid = instance.m_parkedGrid;
				int num = buffer.Length;
				int num2 = buffer2.Length;
				int num3 = vehicleGrid.Length;
				int num4 = vehicleGrid2.Length;
				int num5 = parkedGrid.Length;
				instance.m_vehicles.ClearUnused ();
				instance.m_parkedVehicles.ClearUnused ();
				for (int i = 0; i < num3; i++) {
					vehicleGrid [i] = 0;
				}
				for (int j = 0; j < num4; j++) {
					vehicleGrid2 [j] = 0;
				}
				for (int k = 0; k < num5; k++) {
					parkedGrid [k] = 0;
				}
				for (int l = 0; l < instance.m_updatedParked.Length; l++) {
					instance.m_updatedParked [l] = 0uL;
				}
				instance.m_parkedUpdated = false;
				EncodedArray.UInt uInt = EncodedArray.UInt.BeginRead (s);
				for (int m = 1; m < num; m++) {
					buffer [m].m_flags = (Vehicle.Flags)uInt.Read ();
				}
				uInt.EndRead ();
				if (s.version >= 205u) {
					EncodedArray.UShort uShort = EncodedArray.UShort.BeginRead (s);
					for (int n = 1; n < num2; n++) {
						buffer2 [n].m_flags = uShort.Read ();
					}
					uShort.EndRead ();
				} else {
					if (s.version >= 115u) {
						EncodedArray.UShort uShort2 = EncodedArray.UShort.BeginRead (s);
						for (int num6 = 1; num6 < 65536; num6++) {
							buffer2 [num6].m_flags = uShort2.Read ();
						}
						for (int num7 = 65536; num7 < num2; num7++) {
							buffer2 [num7].m_flags = 0;
						}
						uShort2.EndRead ();
					} else {
						for (int num8 = 1; num8 < num2; num8++) {
							buffer2 [num8].m_flags = 0;
						}
					}
				}
				if (s.version >= 30u) {
					PrefabCollection<VehicleInfo>.BeginDeserialize (s);
					for (int num9 = 1; num9 < num; num9++) {
						if (buffer [num9].m_flags != Vehicle.Flags.None) {
							buffer [num9].m_infoIndex = (ushort)PrefabCollection<VehicleInfo>.Deserialize ();
						}
					}
					if (s.version >= 115u) {
						for (int num10 = 1; num10 < num2; num10++) {
							if (buffer2 [num10].m_flags != 0) {
								buffer2 [num10].m_infoIndex = (ushort)PrefabCollection<VehicleInfo>.Deserialize ();
							}
						}
					}
					PrefabCollection<VehicleInfo>.EndDeserialize (s);
				}
				if (s.version >= 182u) {
					EncodedArray.Byte @byte = EncodedArray.Byte.BeginRead (s);
					for (int num11 = 1; num11 < num; num11++) {
						if (buffer [num11].m_flags != Vehicle.Flags.None) {
							buffer [num11].m_gateIndex = @byte.Read ();
						} else {
							buffer [num11].m_gateIndex = 0;
						}
					}
						@byte.EndRead ();
				} else {
					for (int num12 = 1; num12 < num; num12++) {
						buffer [num12].m_gateIndex = 0;
					}
				}
				for (int num13 = 1; num13 < num; num13++) {
					buffer [num13].m_nextGridVehicle = 0;
					buffer [num13].m_nextGuestVehicle = 0;
					buffer [num13].m_nextOwnVehicle = 0;
					buffer [num13].m_nextLineVehicle = 0;
					buffer [num13].m_leadingVehicle = 0;
					buffer [num13].m_firstCargo = 0;
					buffer [num13].m_nextCargo = 0;
					buffer [num13].m_lastFrame = 0;
					if (buffer [num13].m_flags != Vehicle.Flags.None) {
						buffer [num13].m_frame0 = new Vehicle.Frame (Vector3.zero, Quaternion.identity);
						if (s.version >= 47u) {
							buffer [num13].m_frame0.m_velocity = s.ReadVector3 ();
						}
						buffer [num13].m_frame0.m_position = s.ReadVector3 ();
						if (s.version >= 78u) {
							buffer [num13].m_frame0.m_rotation = s.ReadQuaternion ();
						}
						if (s.version >= 129u) {
							buffer [num13].m_frame0.m_angleVelocity = s.ReadFloat ();
						}
						buffer [num13].m_frame0.m_underground = ((buffer [num13].m_flags & Vehicle.Flags.Underground) != Vehicle.Flags.None);
						buffer [num13].m_frame0.m_transition = ((buffer [num13].m_flags & Vehicle.Flags.Transition) != Vehicle.Flags.None);
						buffer [num13].m_frame1 = buffer [num13].m_frame0;
						buffer [num13].m_frame2 = buffer [num13].m_frame0;
						buffer [num13].m_frame3 = buffer [num13].m_frame0;
						if (s.version >= 68u) {
							buffer [num13].m_targetPos0 = s.ReadVector4 ();
						} else {
							if (s.version >= 47u) {
								buffer [num13].m_targetPos0 = s.ReadVector3 ();
								buffer [num13].m_targetPos0.w = 2f;
							} else {
								buffer [num13].m_targetPos0 = buffer [num13].m_frame0.m_position;
								buffer [num13].m_targetPos0.w = 2f;
							}
						}
						if (s.version >= 90u) {
							buffer [num13].m_targetPos1 = s.ReadVector4 ();
							buffer [num13].m_targetPos2 = s.ReadVector4 ();
							buffer [num13].m_targetPos3 = s.ReadVector4 ();
						} else {
							buffer [num13].m_targetPos1 = buffer [num13].m_targetPos0;
							buffer [num13].m_targetPos2 = buffer [num13].m_targetPos0;
							buffer [num13].m_targetPos3 = buffer [num13].m_targetPos0;
						}
						buffer [num13].m_sourceBuilding = (ushort)s.ReadUInt16 ();
						buffer [num13].m_targetBuilding = (ushort)s.ReadUInt16 ();
						if (s.version >= 52u) {
							buffer [num13].m_transportLine = (ushort)s.ReadUInt16 ();
						} else {
							buffer [num13].m_transportLine = 0;
						}
						buffer [num13].m_transferSize = (ushort)s.ReadUInt16 ();
						buffer [num13].m_transferType = (byte)s.ReadUInt8 ();
						buffer [num13].m_waitCounter = (byte)s.ReadUInt8 ();
						if (s.version >= 99u) {
							buffer [num13].m_blockCounter = (byte)s.ReadUInt8 ();
						} else {
							buffer [num13].m_blockCounter = 0;
						}
						if (s.version >= 32u) {
							buffer [num13].m_citizenUnits = s.ReadUInt24 ();
						} else {
							buffer [num13].m_citizenUnits = 0u;
						}
						if (s.version >= 47u) {
							buffer [num13].m_path = s.ReadUInt24 ();
							buffer [num13].m_pathPositionIndex = (byte)s.ReadUInt8 ();
							buffer [num13].m_lastPathOffset = (byte)s.ReadUInt8 ();
						} else {
							buffer [num13].m_path = 0u;
							buffer [num13].m_pathPositionIndex = 0;
							buffer [num13].m_lastPathOffset = 0;
						}
						if (s.version >= 58u) {
							buffer [num13].m_trailingVehicle = (ushort)s.ReadUInt16 ();
						} else {
							buffer [num13].m_trailingVehicle = 0;
						}
						if (s.version >= 104u) {
							buffer [num13].m_cargoParent = (ushort)s.ReadUInt16 ();
						} else {
							buffer [num13].m_cargoParent = 0;
						}
					} else {
						buffer [num13].m_frame0 = new Vehicle.Frame (Vector3.zero, Quaternion.identity);
						buffer [num13].m_frame1 = new Vehicle.Frame (Vector3.zero, Quaternion.identity);
						buffer [num13].m_frame2 = new Vehicle.Frame (Vector3.zero, Quaternion.identity);
						buffer [num13].m_frame3 = new Vehicle.Frame (Vector3.zero, Quaternion.identity);
						buffer [num13].m_targetPos0 = Vector4.zero;
						buffer [num13].m_targetPos1 = Vector4.zero;
						buffer [num13].m_targetPos2 = Vector4.zero;
						buffer [num13].m_targetPos3 = Vector4.zero;
						buffer [num13].m_sourceBuilding = 0;
						buffer [num13].m_targetBuilding = 0;
						buffer [num13].m_transportLine = 0;
						buffer [num13].m_transferSize = 0;
						buffer [num13].m_transferType = 0;
						buffer [num13].m_waitCounter = 0;
						buffer [num13].m_blockCounter = 0;
						buffer [num13].m_citizenUnits = 0u;
						buffer [num13].m_path = 0u;
						buffer [num13].m_pathPositionIndex = 0;
						buffer [num13].m_lastPathOffset = 0;
						buffer [num13].m_trailingVehicle = 0;
						buffer [num13].m_cargoParent = 0;
						instance.m_vehicles.ReleaseItem ((ushort)num13);
					}
				}
				if (s.version >= 115u) {
					for (int num14 = 1; num14 < num2; num14++) {
						buffer2 [num14].m_nextGridParked = 0;
						buffer2 [num14].m_travelDistance = 0f;
						if (buffer2 [num14].m_flags != 0) {
							buffer2 [num14].m_position = s.ReadVector3 ();
							buffer2 [num14].m_rotation = s.ReadQuaternion ();
							buffer2 [num14].m_ownerCitizen = s.ReadUInt24 ();
							if ((buffer2 [num14].m_flags & 4) != 0) {
								instance.m_updatedParked [num14 >> 6] |= 1uL << num14;
								instance.m_parkedUpdated = true;
							}
						} else {
							buffer2 [num14].m_position = Vector3.zero;
							buffer2 [num14].m_rotation = Quaternion.identity;
							buffer2 [num14].m_ownerCitizen = 0u;
							instance.m_parkedVehicles.ReleaseItem ((ushort)num14);
						}
					}
				} else {
					for (int num15 = 1; num15 < num2; num15++) {
						buffer2 [num15].m_nextGridParked = 0;
						buffer2 [num15].m_travelDistance = 0f;
						buffer2 [num15].m_position = Vector3.zero;
						buffer2 [num15].m_rotation = Quaternion.identity;
						buffer2 [num15].m_ownerCitizen = 0u;
						instance.m_parkedVehicles.ReleaseItem ((ushort)num15);
					}
				}
				Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.EndDeserialize (s, "FakeVehicleManager");
			}

			public void AfterDeserialize (DataSerializer s)
			{
				Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.BeginAfterDeserialize (s, "FakeVehicleManager");
				CitizenManager instance = Singleton<CitizenManager>.instance;
				FakeVehicleManager instance2 = FakeVehicleManager.instance;
				Singleton<LoadingManager>.instance.WaitUntilEssentialScenesLoaded ();
				PrefabCollection<VehicleInfo>.BindPrefabs ();
				instance2.RefreshTransferVehicles ();
				Vehicle[] buffer = instance2.m_vehicles.m_buffer;
				VehicleParked[] buffer2 = instance2.m_parkedVehicles.m_buffer;
				int num = buffer.Length;
				int num2 = buffer2.Length;
				for (int i = 1; i < num; i++) {
					if (buffer [i].m_flags != Vehicle.Flags.None) {
						ushort trailingVehicle = buffer [i].m_trailingVehicle;
						if (trailingVehicle != 0) {
							if (buffer [(int)trailingVehicle].m_flags != Vehicle.Flags.None) {
								buffer [(int)trailingVehicle].m_leadingVehicle = (ushort)i;
							} else {
								buffer [i].m_trailingVehicle = 0;
							}
						}
						ushort cargoParent = buffer [i].m_cargoParent;
						if (cargoParent != 0) {
							if (buffer [(int)cargoParent].m_flags != Vehicle.Flags.None) {
								buffer [i].m_nextCargo = buffer [(int)cargoParent].m_firstCargo;
								buffer [(int)cargoParent].m_firstCargo = (ushort)i;
							} else {
								buffer [i].m_cargoParent = 0;
							}
						}
					}
				}
				for (int j = 1; j < num; j++) {
					if (buffer [j].m_flags != Vehicle.Flags.None) {
						if (buffer [j].m_path != 0u) {
							PathUnit[] expr_17F_cp_0 = Singleton<PathManager>.instance.m_pathUnits.m_buffer;
							UIntPtr expr_17F_cp_1 = (UIntPtr)buffer [j].m_path;
							expr_17F_cp_0 [(int)expr_17F_cp_1].m_referenceCount = (byte)(expr_17F_cp_0 [(int)expr_17F_cp_1].m_referenceCount + 1);
						}
						VehicleInfo info = buffer [j].Info;
						if (info != null) {
							buffer [j].m_infoIndex = (ushort)info.m_prefabDataIndex;
							info.m_vehicleAI.LoadVehicle ((ushort)j, ref buffer [j]);
							if ((buffer [j].m_flags & Vehicle.Flags.Spawned) != Vehicle.Flags.None) {
								instance2.AddToGrid ((ushort)j, ref buffer [j], info.m_isLargeVehicle);
							}
						}
						uint num3 = buffer [j].m_citizenUnits;
						int num4 = 0;
						while (num3 != 0u) {
							instance.m_units.m_buffer [(int)((UIntPtr)num3)].SetVehicleAfterLoading ((ushort)j);
							num3 = instance.m_units.m_buffer [(int)((UIntPtr)num3)].m_nextUnit;
							if (++num4 > 524288) {
								CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
								break;
							}
						}
					}
				}
				for (int k = 1; k < num2; k++) {
					if (buffer2 [k].m_flags != 0) {
						VehicleInfo info2 = buffer2 [k].Info;
						if (info2 != null) {
							buffer2 [k].m_infoIndex = (ushort)info2.m_prefabDataIndex;
							instance2.AddToGrid ((ushort)k, ref buffer2 [k]);
						}
						uint ownerCitizen = buffer2 [k].m_ownerCitizen;
						if (ownerCitizen != 0u) {
							instance.m_citizens.m_buffer [(int)((UIntPtr)ownerCitizen)].m_parkedVehicle = (ushort)k;
						}
					}
				}
				instance2.m_vehicleCount = (int)(instance2.m_vehicles.ItemCount () - 1u);
				instance2.m_parkedCount = (int)(instance2.m_parkedVehicles.ItemCount () - 1u);
				Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.EndAfterDeserialize (s, "FakeVehicleManager");
			}
		}

		private sealed class RendererIterator : IEnumerator, IDisposable, IEnumerator<object>
		{
			//
			// Fields
			//
			internal Texture2D xys__15;
			internal Texture2D aci__16;
			internal Texture2D rgb__14;
			internal int j__12;
			internal VehicleInfoBase subInfo__13;
			internal PrefabException e__17;
			internal object _current;
			internal FakeVehicleManager f__this;
			internal int _PC;
			internal Rect[] rect__18;
			internal int i__19;
			internal PrefabException e__11;
			internal FastList<Texture2D> aciTextures__3;
			internal int vehicleCount__4;
			internal FastList<Texture2D> xysTextures__2;
			internal FastList<VehicleInfoBase> infos__0;
			internal FastList<Texture2D> rgbTextures__1;
			internal int i__5;
			internal Texture2D xys__9;
			internal Texture2D aci__10;
			internal Texture2D rgb__8;
			internal VehicleInfo info__6;
			internal PrefabException e__7;
		
			//
			// Properties
			//
			object IEnumerator.Current {
				[DebuggerHidden]
			get {
					return this._current;
				}
			}
		
			object IEnumerator<object>.Current {
				[DebuggerHidden]
			get {
					return this._current;
				}
			}
		
			//
			// Methods
			//
			[DebuggerHidden]
			public void Dispose ()
			{
				this._PC = -1;
			}
		
			public bool MoveNext ()
			{
				uint num = (uint)this._PC;
				this._PC = -1;
				switch (num) {
				case 0u:
					Singleton<LoadingManager>.instance.m_loadingProfilerMain.BeginLoading ("FakeVehicleManager.InitRenderData");
					this.infos__0 = new FastList<VehicleInfoBase> ();
					this.rgbTextures__1 = new FastList<Texture2D> ();
					this.xysTextures__2 = new FastList<Texture2D> ();
					this.aciTextures__3 = new FastList<Texture2D> ();
					this.vehicleCount__4 = PrefabCollection<VehicleInfo>.LoadedCount ();
					this.infos__0.EnsureCapacity (this.vehicleCount__4 * 2);
					this.rgbTextures__1.EnsureCapacity (this.vehicleCount__4 * 2);
					this.xysTextures__2.EnsureCapacity (this.vehicleCount__4 * 2);
					this.aciTextures__3.EnsureCapacity (this.vehicleCount__4 * 2);
					this.i__5 = 0;
					while (this.i__5 < this.vehicleCount__4) {
						this.info__6 = PrefabCollection<VehicleInfo>.GetLoaded ((uint)this.i__5);
						if (this.info__6 != null) {
							try {
								this.info__6.CheckReferences ();
							} catch (PrefabException ex) {
								this.e__7 = ex;
								CODebugBase<LogChannel>.Error (LogChannel.Core, string.Concat (new string[]
							                                                               {
								this.e__7.m_prefabInfo.gameObject.name,
								": ",
								this.e__7.Message,
								"\n",
								this.e__7.StackTrace
							}), this.e__7.m_prefabInfo.gameObject);
								LoadingManager expr_177 = Singleton<LoadingManager>.instance;
								string brokenAssets = expr_177.m_brokenAssets;
								expr_177.m_brokenAssets = string.Concat (new string[]
							                                         {
								brokenAssets,
								"\n",
								this.e__7.m_prefabInfo.gameObject.name,
								": ",
								this.e__7.Message
							});
							}
							if (!this.info__6.m_hasLodData) {
								try {
									this.info__6.m_hasLodData = true;
									if (this.info__6.m_lodMesh == null || this.info__6.m_lodMaterial == null) {
										this.info__6.InitMeshData (new Rect (0f, 0f, 1f, 1f), null, null, null);
									} else {
										this.rgb__8 = null;
										if (this.info__6.m_lodMaterial.HasProperty (this.f__this.ID_MainTex)) {
											this.rgb__8 = (this.info__6.m_lodMaterial.GetTexture (this.f__this.ID_MainTex) as Texture2D);
										}
										this.xys__9 = null;
										if (this.info__6.m_lodMaterial.HasProperty (this.f__this.ID_XYSMap)) {
											this.xys__9 = (this.info__6.m_lodMaterial.GetTexture (this.f__this.ID_XYSMap) as Texture2D);
										}
										this.aci__10 = null;
										if (this.info__6.m_lodMaterial.HasProperty (this.f__this.ID_ACIMap)) {
											this.aci__10 = (this.info__6.m_lodMaterial.GetTexture (this.f__this.ID_ACIMap) as Texture2D);
										}
										if (this.rgb__8 == null && this.xys__9 == null && this.aci__10 == null && this.info__6.m_material.mainTexture == null) {
											this.info__6.InitMeshData (new Rect (0f, 0f, 1f, 1f), null, null, null);
										} else {
											if (this.rgb__8 == null) {
												throw new PrefabException (this.info__6, "LOD diffuse null");
											}
											if (this.xys__9 == null) {
												throw new PrefabException (this.info__6, "LOD xys null");
											}
											if (this.aci__10 == null) {
												throw new PrefabException (this.info__6, "LOD aci null");
											}
											if (this.xys__9.width != this.rgb__8.width || this.xys__9.height != this.rgb__8.height) {
												throw new PrefabException (this.info__6, "LOD xys size doesnt match diffuse size");
											}
											if (this.aci__10.width != this.rgb__8.width || this.aci__10.height != this.rgb__8.height) {
												throw new PrefabException (this.info__6, "LOD aci size doesnt match diffuse size");
											}
											try {
												this.rgb__8.GetPixel (0, 0);
											} catch (UnityException) {
												throw new PrefabException (this.info__6, "LOD diffuse not readable");
											}
											try {
												this.xys__9.GetPixel (0, 0);
											} catch (UnityException) {
												throw new PrefabException (this.info__6, "LOD xys not readable");
											}
											try {
												this.aci__10.GetPixel (0, 0);
											} catch (UnityException) {
												throw new PrefabException (this.info__6, "LOD aci not readable");
											}
											this.infos__0.Add (this.info__6);
											this.rgbTextures__1.Add (this.rgb__8);
											this.xysTextures__2.Add (this.xys__9);
											this.aciTextures__3.Add (this.aci__10);
										}
									}
								} catch (PrefabException ex2) {
									this.e__11 = ex2;
									CODebugBase<LogChannel>.Error (LogChannel.Core, string.Concat (new string[]
								                                                               {
									this.e__11.m_prefabInfo.gameObject.name,
									": ",
									this.e__11.Message,
									"\n",
									this.e__11.StackTrace
								}), this.e__11.m_prefabInfo.gameObject);
									LoadingManager expr_5CF = Singleton<LoadingManager>.instance;
									string brokenAssets = expr_5CF.m_brokenAssets;
									expr_5CF.m_brokenAssets = string.Concat (new string[]
								                                         {
									brokenAssets,
									"\n",
									this.e__11.m_prefabInfo.gameObject.name,
									": ",
									this.e__11.Message
								});
								}
							}
							if (this.info__6.m_subMeshes != null) {
								this.j__12 = 0;
								while (this.j__12 < this.info__6.m_subMeshes.Length) {
									try {
										this.subInfo__13 = this.info__6.m_subMeshes [this.j__12].m_subInfo;
										if (this.subInfo__13 != null && !this.subInfo__13.m_hasLodData) {
											this.subInfo__13.m_hasLodData = true;
											if (this.subInfo__13.m_lodMesh == null || this.subInfo__13.m_lodMaterial == null) {
												this.subInfo__13.InitMeshData (new Rect (0f, 0f, 1f, 1f), null, null, null);
											} else {
												this.rgb__14 = null;
												if (this.subInfo__13.m_lodMaterial.HasProperty (this.f__this.ID_MainTex)) {
													this.rgb__14 = (this.subInfo__13.m_lodMaterial.GetTexture (this.f__this.ID_MainTex) as Texture2D);
												}
												this.xys__15 = null;
												if (this.subInfo__13.m_lodMaterial.HasProperty (this.f__this.ID_XYSMap)) {
													this.xys__15 = (this.subInfo__13.m_lodMaterial.GetTexture (this.f__this.ID_XYSMap) as Texture2D);
												}
												this.aci__16 = null;
												if (this.subInfo__13.m_lodMaterial.HasProperty (this.f__this.ID_ACIMap)) {
													this.aci__16 = (this.subInfo__13.m_lodMaterial.GetTexture (this.f__this.ID_ACIMap) as Texture2D);
												}
												if (this.rgb__14 == null && this.xys__15 == null && this.aci__16 == null && this.subInfo__13.m_material.mainTexture == null) {
													this.subInfo__13.InitMeshData (new Rect (0f, 0f, 1f, 1f), null, null, null);
												} else {
													if (this.rgb__14 == null) {
														throw new PrefabException (this.subInfo__13, "LOD diffuse null");
													}
													if (this.xys__15 == null) {
														throw new PrefabException (this.subInfo__13, "LOD xys null");
													}
													if (this.aci__16 == null) {
														throw new PrefabException (this.subInfo__13, "LOD aci null");
													}
													if (this.xys__15.width != this.rgb__14.width || this.xys__15.height != this.rgb__14.height) {
														throw new PrefabException (this.subInfo__13, "LOD xys size not match diffuse size");
													}
													if (this.aci__16.width != this.rgb__14.width || this.aci__16.height != this.rgb__14.height) {
														throw new PrefabException (this.subInfo__13, "LOD aci size not match diffuse size");
													}
													try {
														this.rgb__14.GetPixel (0, 0);
													} catch (UnityException) {
														throw new PrefabException (this.subInfo__13, "LOD diffuse not readable");
													}
													try {
														this.xys__15.GetPixel (0, 0);
													} catch (UnityException) {
														throw new PrefabException (this.subInfo__13, "LOD xys not readable");
													}
													try {
														this.aci__16.GetPixel (0, 0);
													} catch (UnityException) {
														throw new PrefabException (this.subInfo__13, "LOD aci not readable");
													}
													this.infos__0.Add (this.subInfo__13);
													this.rgbTextures__1.Add (this.rgb__14);
													this.xysTextures__2.Add (this.xys__15);
													this.aciTextures__3.Add (this.aci__16);
												}
											}
										}
									} catch (PrefabException ex3) {
										this.e__17 = ex3;
										CODebugBase<LogChannel>.Error (LogChannel.Core, string.Concat (new string[]
									                                                               {
										this.e__17.m_prefabInfo.gameObject.name,
										": ",
										this.e__17.Message,
										"\n",
										this.e__17.StackTrace
									}), this.e__17.m_prefabInfo.gameObject);
										LoadingManager expr_A73 = Singleton<LoadingManager>.instance;
										string brokenAssets = expr_A73.m_brokenAssets;
										expr_A73.m_brokenAssets = string.Concat (new string[]
									                                         {
										brokenAssets,
										"\n",
										this.e__17.m_prefabInfo.gameObject.name,
										": ",
										this.e__17.Message
									});
									}
									this.j__12++;
								}
							}
						}
						this.i__5++;
					}
					if (FakeVehicleManager.instance.m_lodRgbAtlas == null) {
						FakeVehicleManager.instance.m_lodRgbAtlas = new Texture2D (1024, 1024, TextureFormat.DXT1, true, false);
						FakeVehicleManager.instance.m_lodRgbAtlas.filterMode = FilterMode.Trilinear;
						FakeVehicleManager.instance.m_lodRgbAtlas.anisoLevel = 4;
					}
					if (FakeVehicleManager.instance.m_lodXysAtlas == null) {
						FakeVehicleManager.instance.m_lodXysAtlas = new Texture2D (1024, 1024, TextureFormat.DXT1, true, true);
						FakeVehicleManager.instance.m_lodXysAtlas.filterMode = FilterMode.Trilinear;
						FakeVehicleManager.instance.m_lodXysAtlas.anisoLevel = 4;
					}
					if (FakeVehicleManager.instance.m_lodAciAtlas == null) {
						FakeVehicleManager.instance.m_lodAciAtlas = new Texture2D (1024, 1024, TextureFormat.DXT1, true, true);
						FakeVehicleManager.instance.m_lodAciAtlas.filterMode = FilterMode.Trilinear;
						FakeVehicleManager.instance.m_lodAciAtlas.anisoLevel = 4;
					}
					Singleton<LoadingManager>.instance.m_loadingProfilerMain.PauseLoading ();
					this._current = 0;
					this._PC = 1;
					return true;
				case 1u:
					Singleton<LoadingManager>.instance.m_loadingProfilerMain.ContinueLoading ();
					this.rect__18 = FakeVehicleManager.instance.m_lodRgbAtlas.PackTextures (this.rgbTextures__1.ToArray (), 0, 4096, false);
					Singleton<LoadingManager>.instance.m_loadingProfilerMain.PauseLoading ();
					this._current = 0;
					this._PC = 2;
					return true;
				case 2u:
					Singleton<LoadingManager>.instance.m_loadingProfilerMain.ContinueLoading ();
					FakeVehicleManager.instance.m_lodXysAtlas.PackTextures (this.xysTextures__2.ToArray (), 0, 4096, false);
					Singleton<LoadingManager>.instance.m_loadingProfilerMain.PauseLoading ();
					this._current = 0;
					this._PC = 3;
					return true;
				case 3u:
					Singleton<LoadingManager>.instance.m_loadingProfilerMain.ContinueLoading ();
					FakeVehicleManager.instance.m_lodAciAtlas.PackTextures (this.aciTextures__3.ToArray (), 0, 4096, false);
					Singleton<LoadingManager>.instance.m_loadingProfilerMain.PauseLoading ();
					this._current = 0;
					this._PC = 4;
					return true;
				case 4u:
					Singleton<LoadingManager>.instance.m_loadingProfilerMain.ContinueLoading ();
					this.i__19 = 0;
					while (this.i__19 < this.infos__0.m_size) {
						this.infos__0.m_buffer [this.i__19].InitMeshData (this.rect__18 [this.i__19], FakeVehicleManager.instance.m_lodRgbAtlas, FakeVehicleManager.instance.m_lodXysAtlas, FakeVehicleManager.instance.m_lodAciAtlas);
						this.i__19++;
					}
					Singleton<LoadingManager>.instance.m_loadingProfilerMain.EndLoading ();
					this._current = 0;
					this._PC = 5;
					return true;
				case 5u:
					this._PC = -1;
					break;
				}
				return false;
			}
		
			[DebuggerHidden]
			public void Reset ()
			{
				throw new NotSupportedException ();
			}
	
		}
	}
}