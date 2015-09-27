using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using System;
using UnityEngine;

namespace Klyte.Unlimiter.Fake
{
	public class FakeCargoTruckAI
	{
		private static ushort FindCargoParent (ushort sourceBuilding, ushort targetBuilding, ItemClass.Service service, ItemClass.SubService subService)
		{
			BuildingManager instance = Singleton<BuildingManager>.instance;
			VehicleManager instance2 = Singleton<VehicleManager>.instance;
			ushort num = instance.m_buildings.m_buffer [(int)sourceBuilding].m_ownVehicles;
			int num2 = 0;
			while (num != 0)
			{
				if (instance2.m_vehicles.m_buffer [(int)num].m_targetBuilding == targetBuilding && (instance2.m_vehicles.m_buffer [(int)num].m_flags & Vehicle.Flags.WaitingCargo) != Vehicle.Flags.None)
				{
					VehicleInfo info = instance2.m_vehicles.m_buffer [(int)num].Info;
					if (info.m_class.m_service == service && info.m_class.m_subService == subService)
					{
						int num3;
						int num4;
						info.m_vehicleAI.GetSize (num, ref instance2.m_vehicles.m_buffer [(int)num], out num3, out num4);
						if (num3 < num4)
						{
							return num;
						}
					}
				}
				num = instance2.m_vehicles.m_buffer [(int)num].m_nextOwnVehicle;
				if (++num2 >= 65536)
				{
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			return 0;
		}
		public static ushort FindNextCargoParent (ushort sourceBuilding, ItemClass.Service service, ItemClass.SubService subService)
		{
			BuildingManager instance = Singleton<BuildingManager>.instance;
			VehicleManager instance2 = Singleton<VehicleManager>.instance;
			ushort num = instance.m_buildings.m_buffer [(int)sourceBuilding].m_ownVehicles;
			ushort result = 0;
			int num2 = -1;
			int num3 = 0;
			while (num != 0) {
				if ((instance2.m_vehicles.m_buffer [(int)num].m_flags & Vehicle.Flags.WaitingCargo) != Vehicle.Flags.None) {
					VehicleInfo info = instance2.m_vehicles.m_buffer [(int)num].Info;
					if (info.m_class.m_service == service && info.m_class.m_subService == subService) {
						int num4;
						int b;
						info.m_vehicleAI.GetSize (num, ref instance2.m_vehicles.m_buffer [(int)num], out num4, out b);
						int num5 = Mathf.Max (num4 * 255 / Mathf.Max (1, b), (int)instance2.m_vehicles.m_buffer [(int)num].m_waitCounter);
						if (num5 > num2) {
							result = num;
							num2 = num5;
						}
					}
				}
				num = instance2.m_vehicles.m_buffer [(int)num].m_nextOwnVehicle;
				if (++num3 >= 65536) {
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			return result;
		}
	
		public static void SwitchCargoParent (ushort source, ushort target)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			ushort num = instance.m_vehicles.m_buffer [(int)source].m_firstCargo;
			instance.m_vehicles.m_buffer [(int)source].m_firstCargo = 0;
			instance.m_vehicles.m_buffer [(int)target].m_firstCargo = num;
			instance.m_vehicles.m_buffer [(int)target].m_transferSize = instance.m_vehicles.m_buffer [(int)source].m_transferSize;
			int num2 = 0;
			while (num != 0) {
				instance.m_vehicles.m_buffer [(int)num].m_cargoParent = target;
				num = instance.m_vehicles.m_buffer [(int)num].m_nextCargo;
				if (++num2 > 65536) {
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			instance.ReleaseVehicle (source);
		}
	}
}

