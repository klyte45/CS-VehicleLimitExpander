using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using ColossalFramework.Steamworks;
using ColossalFramework.Threading;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Klyte.Unlimiter.Fake
{
	public class FakeTouristAI : HumanAI
	{

		private bool DoRandomMove ()
		{
			uint vehicleCount = (uint)Singleton<VehicleManager>.instance.m_vehicleCount;
			uint instanceCount = (uint)Singleton<CitizenManager>.instance.m_instanceCount;
			if (vehicleCount * 65536u > instanceCount * 65536u) {
				return Singleton<SimulationManager>.instance.m_randomizer.UInt32 (65536u) > vehicleCount;
			}
			return Singleton<SimulationManager>.instance.m_randomizer.UInt32 (65536u) > instanceCount;
		}

		protected override bool SpawnVehicle (ushort instanceID, ref CitizenInstance citizenData, PathUnit.Position pathPos)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			float num = 20f;
			int num2 = Mathf.Max ((int)((citizenData.m_targetPos.x - num) / 32f + 270f), 0);
			int num3 = Mathf.Max ((int)((citizenData.m_targetPos.z - num) / 32f + 270f), 0);
			int num4 = Mathf.Min ((int)((citizenData.m_targetPos.x + num) / 32f + 270f), 539);
			int num5 = Mathf.Min ((int)((citizenData.m_targetPos.z + num) / 32f + 270f), 539);
			for (int i = num3; i <= num5; i++)
			{
				for (int j = num2; j <= num4; j++)
				{
					ushort num6 = instance.m_vehicleGrid [i * 540 + j];
					int num7 = 0;
					while (num6 != 0)
					{
						if (this.TryJoinVehicle (instanceID, ref citizenData, num6, ref instance.m_vehicles.m_buffer [(int)num6]))
						{
							citizenData.m_flags |= CitizenInstance.Flags.EnteringVehicle;
							citizenData.m_flags &= ~CitizenInstance.Flags.TryingSpawnVehicle;
							citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;
							citizenData.m_waitCounter = 0;
							return true;
						}
						num6 = instance.m_vehicles.m_buffer [(int)num6].m_nextGridVehicle;
						if (++num7 > 65536)
						{
							CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
							break;
						}
					}
				}
			}
			NetManager instance2 = Singleton<NetManager>.instance;
			CitizenManager instance3 = Singleton<CitizenManager>.instance;
			Vector3 vector = Vector3.zero;
			Quaternion rotation = Quaternion.identity;
			ushort num8 = instance3.m_citizens.m_buffer [(int)((UIntPtr)citizenData.m_citizen)].m_parkedVehicle;
			if (num8 != 0)
			{
				vector = instance.m_parkedVehicles.m_buffer [(int)num8].m_position;
				rotation = instance.m_parkedVehicles.m_buffer [(int)num8].m_rotation;
			}
			VehicleInfo vehicleInfo = this.GetVehicleInfo (instanceID, ref citizenData, false);
			if (vehicleInfo == null || vehicleInfo.m_vehicleType == VehicleInfo.VehicleType.Bicycle)
			{
				instance3.m_citizens.m_buffer [(int)((UIntPtr)citizenData.m_citizen)].SetParkedVehicle (citizenData.m_citizen, 0);
				if ((citizenData.m_flags & CitizenInstance.Flags.TryingSpawnVehicle) == CitizenInstance.Flags.None)
				{
					citizenData.m_flags |= CitizenInstance.Flags.TryingSpawnVehicle;
					citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;
					citizenData.m_waitCounter = 0;
				}
				return true;
			}
			if (vehicleInfo.m_class.m_subService == ItemClass.SubService.PublicTransportTaxi)
			{
				instance3.m_citizens.m_buffer [(int)((UIntPtr)citizenData.m_citizen)].SetParkedVehicle (citizenData.m_citizen, 0);
				if ((citizenData.m_flags & CitizenInstance.Flags.WaitingTaxi) == CitizenInstance.Flags.None)
				{
					citizenData.m_flags |= CitizenInstance.Flags.WaitingTaxi;
					citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;
					citizenData.m_waitCounter = 0;
				}
				return true;
			}
			uint laneID = PathManager.GetLaneID (pathPos);
			Vector3 vector2 = citizenData.m_targetPos;
			if (num8 != 0 && Vector3.SqrMagnitude (vector - vector2) < 1024f)
			{
				vector2 = vector;
			}
			else
			{
				num8 = 0;
			}
			Vector3 a;
			float num9;
			instance2.m_lanes.m_buffer [(int)((UIntPtr)laneID)].GetClosestPosition (vector2, out a, out num9);
			byte lastPathOffset = (byte)Mathf.Clamp (Mathf.RoundToInt (num9 * 255f), 0, 255);
			a = vector2 + Vector3.ClampMagnitude (a - vector2, 5f);
			ushort num10;
			if (instance.CreateVehicle (out num10, ref Singleton<SimulationManager>.instance.m_randomizer, vehicleInfo, vector2, TransferManager.TransferReason.None, false, false))
			{
				Vehicle.Frame frame = instance.m_vehicles.m_buffer [(int)num10].m_frame0;
				if (num8 != 0)
				{
					frame.m_rotation = rotation;
				}
				else
				{
					Vector3 forward = a - citizenData.GetLastFrameData ().m_position;
					if (forward.sqrMagnitude > 0.01f)
					{
						frame.m_rotation = Quaternion.LookRotation (forward);
					}
				}
				instance.m_vehicles.m_buffer [(int)num10].m_frame0 = frame;
				instance.m_vehicles.m_buffer [(int)num10].m_frame1 = frame;
				instance.m_vehicles.m_buffer [(int)num10].m_frame2 = frame;
				instance.m_vehicles.m_buffer [(int)num10].m_frame3 = frame;
				vehicleInfo.m_vehicleAI.FrameDataUpdated (num10, ref instance.m_vehicles.m_buffer [(int)num10], ref frame);
				instance.m_vehicles.m_buffer [(int)num10].m_targetPos0 = new Vector4 (a.x, a.y, a.z, 2f);
				Vehicle[] expr_4E5_cp_0 = instance.m_vehicles.m_buffer;
				ushort expr_4E5_cp_1 = num10;
				expr_4E5_cp_0 [(int)expr_4E5_cp_1].m_flags = (expr_4E5_cp_0 [(int)expr_4E5_cp_1].m_flags | Vehicle.Flags.Stopped);
				instance.m_vehicles.m_buffer [(int)num10].m_path = citizenData.m_path;
				instance.m_vehicles.m_buffer [(int)num10].m_pathPositionIndex = citizenData.m_pathPositionIndex;
				instance.m_vehicles.m_buffer [(int)num10].m_lastPathOffset = lastPathOffset;
				instance.m_vehicles.m_buffer [(int)num10].m_transferSize = (ushort)(citizenData.m_citizen & 65535u);
				vehicleInfo.m_vehicleAI.TrySpawn (num10, ref instance.m_vehicles.m_buffer [(int)num10]);
				if (num8 != 0)
				{
					InstanceID empty = InstanceID.Empty;
					empty.ParkedVehicle = num8;
					InstanceID empty2 = InstanceID.Empty;
					empty2.Vehicle = num10;
					Singleton<InstanceManager>.instance.ChangeInstance (empty, empty2);
				}
				citizenData.m_path = 0u;
				instance3.m_citizens.m_buffer [(int)((UIntPtr)citizenData.m_citizen)].SetParkedVehicle (citizenData.m_citizen, 0);
				instance3.m_citizens.m_buffer [(int)((UIntPtr)citizenData.m_citizen)].SetVehicle (citizenData.m_citizen, num10, 0u);
				citizenData.m_flags |= CitizenInstance.Flags.EnteringVehicle;
				citizenData.m_flags &= ~CitizenInstance.Flags.TryingSpawnVehicle;
				citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;
				citizenData.m_waitCounter = 0;
				return true;
			}
			instance3.m_citizens.m_buffer [(int)((UIntPtr)citizenData.m_citizen)].SetParkedVehicle (citizenData.m_citizen, 0);
			if ((citizenData.m_flags & CitizenInstance.Flags.TryingSpawnVehicle) == CitizenInstance.Flags.None)
			{
				citizenData.m_flags |= CitizenInstance.Flags.TryingSpawnVehicle;
				citizenData.m_flags &= ~CitizenInstance.Flags.BoredOfWaiting;
				citizenData.m_waitCounter = 0;
			}
			return true;
		}
		private bool TryJoinVehicle (ushort instanceID, ref CitizenInstance citizenData, ushort vehicleID, ref Vehicle vehicleData)
		{
			if ((vehicleData.m_flags & Vehicle.Flags.Stopped) == Vehicle.Flags.None)
			{
				return false;
			}
			CitizenManager instance = Singleton<CitizenManager>.instance;
			uint num = vehicleData.m_citizenUnits;
			int num2 = 0;
			while (num != 0u)
			{
				uint nextUnit = instance.m_units.m_buffer [(int)((UIntPtr)num)].m_nextUnit;
				for (int i = 0; i < 5; i++)
				{
					uint citizen = instance.m_units.m_buffer [(int)((UIntPtr)num)].GetCitizen (i);
					if (citizen != 0u)
					{
						ushort instance2 = instance.m_citizens.m_buffer [(int)((UIntPtr)citizen)].m_instance;
						if (instance2 != 0 && instance.m_instances.m_buffer [(int)instance2].m_targetBuilding == citizenData.m_targetBuilding)
						{
							instance.m_citizens.m_buffer [(int)((UIntPtr)citizenData.m_citizen)].SetVehicle (citizenData.m_citizen, vehicleID, 0u);
							if (instance.m_citizens.m_buffer [(int)((UIntPtr)citizenData.m_citizen)].m_vehicle == vehicleID)
							{
								if (citizenData.m_path != 0u)
								{
									Singleton<PathManager>.instance.ReleasePath (citizenData.m_path);
									citizenData.m_path = 0u;
								}
								return true;
							}
						}
						break;
					}
				}
				num = nextUnit;
				if (++num2 > 524288)
				{
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			return false;
		}
		protected override VehicleInfo GetVehicleInfo (ushort instanceID, ref CitizenInstance citizenData, bool forceProbability)
		{
			if (citizenData.m_citizen == 0u)
			{
				return null;
			}
			int num;
			int num2;
			int num3;
			if (forceProbability || (citizenData.m_flags & CitizenInstance.Flags.BorrowCar) != CitizenInstance.Flags.None)
			{
				num = 100;
				num2 = 0;
				num3 = 0;
			}
			else
			{
				num = this.GetCarProbability ();
				num2 = this.GetBikeProbability ();
				num3 = this.GetTaxiProbability ();
			}
			Randomizer randomizer = new Randomizer (citizenData.m_citizen);
			bool flag = randomizer.Int32 (100u) < num;
			bool flag2 = randomizer.Int32 (100u) < num2;
			bool flag3 = randomizer.Int32 (100u) < num3;
			ItemClass.Service service = ItemClass.Service.Residential;
			ItemClass.SubService subService = ItemClass.SubService.ResidentialLow;
			if (!flag && flag3)
			{
				service = ItemClass.Service.PublicTransport;
				subService = ItemClass.SubService.PublicTransportTaxi;
			}
			VehicleInfo randomVehicleInfo = Singleton<VehicleManager>.instance.GetRandomVehicleInfo (ref randomizer, service, subService, ItemClass.Level.Level1);
			VehicleInfo randomVehicleInfo2 = Singleton<VehicleManager>.instance.GetRandomVehicleInfo (ref randomizer, ItemClass.Service.Residential, ItemClass.SubService.ResidentialHigh, ItemClass.Level.Level2);
			if (flag2 && randomVehicleInfo2 != null)
			{
				return randomVehicleInfo2;
			}
			if ((flag || flag3) && randomVehicleInfo != null)
			{
				return randomVehicleInfo;
			}
			return null;
		}
		
		private int GetBikeProbability ()
		{
			return 20;
		}
		
		private int GetCarProbability ()
		{
			return 20;
		}
		private int GetTaxiProbability ()
		{
			return 20;
		}
	}

}