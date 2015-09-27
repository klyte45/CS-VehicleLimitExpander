using ColossalFramework;
using ColossalFramework.Math;
using System;
using UnityEngine;

namespace Klyte.Unlimiter.Fake
{
	public class FakeTransportStationAI : FakeDepotAI
	{

		private ushort FindConnectionVehicle (ushort buildingID, ref Building buildingData, ushort targetStop, float maxDistance)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			Vector3 position = Singleton<NetManager>.instance.m_nodes.m_buffer [(int)targetStop].m_position;
			ushort num = buildingData.m_ownVehicles;
			int num2 = 0;
			while (num != 0) {
				if (instance.m_vehicles.m_buffer [(int)num].m_transportLine == 0) {
					VehicleInfo info = instance.m_vehicles.m_buffer [(int)num].Info;
					if (info.m_class.m_service == this.m_info.m_class.m_service && info.m_class.m_subService == this.m_info.m_class.m_subService && instance.m_vehicles.m_buffer [(int)num].m_targetBuilding == targetStop && Vector3.SqrMagnitude (instance.m_vehicles.m_buffer [(int)num].GetLastFramePosition () - position) < maxDistance * maxDistance) {
						return num;
					}
				}
				num = instance.m_vehicles.m_buffer [(int)num].m_nextOwnVehicle;
				if (++num2 > 65536) {
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			return 0;
		}

		private void ReleaseVehicles (ushort buildingID, ref Building data)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			ushort num = data.m_ownVehicles;
			int num2 = 0;
			while (num != 0) {
				if (instance.m_vehicles.m_buffer [(int)num].m_transportLine == 0) {
					VehicleInfo info = instance.m_vehicles.m_buffer [(int)num].Info;
					if (info.m_class.m_service == this.m_info.m_class.m_service && info.m_class.m_subService == this.m_info.m_class.m_subService) {
						info.m_vehicleAI.SetTarget (num, ref instance.m_vehicles.m_buffer [(int)num], 0);
					}
				}
				num = instance.m_vehicles.m_buffer [(int)num].m_nextOwnVehicle;
				if (++num2 > 65536) {
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
		}
	}
}

