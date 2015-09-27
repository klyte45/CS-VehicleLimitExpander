using ColossalFramework;
using ColossalFramework.Math;
using System;
using UnityEngine;

namespace Klyte.Unlimiter.Fake
{
public class FakeCommonBuildingAI 
{

	protected void CalculateGuestVehicles (ushort buildingID, ref Building data, TransferManager.TransferReason material, ref int count, ref int cargo, ref int capacity, ref int outside)
	{
		VehicleManager instance = Singleton<VehicleManager>.instance;
		ushort num = data.m_guestVehicles;
		int num2 = 0;
		while (num != 0)
		{
			if ((TransferManager.TransferReason)instance.m_vehicles.m_buffer [(int)num].m_transferType == material)
			{
				VehicleInfo info = instance.m_vehicles.m_buffer [(int)num].Info;
				int a;
				int num3;
				info.m_vehicleAI.GetSize (num, ref instance.m_vehicles.m_buffer [(int)num], out a, out num3);
				cargo += Mathf.Min (a, num3);
				capacity += num3;
				count++;
				if ((instance.m_vehicles.m_buffer [(int)num].m_flags & (Vehicle.Flags.Importing | Vehicle.Flags.Exporting)) != Vehicle.Flags.None)
				{
					outside++;
				}
			}
			num = instance.m_vehicles.m_buffer [(int)num].m_nextGuestVehicle;
			if (++num2 > 16384)
			{
				CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
				break;
			}
		}
	}
	
	protected void CalculateOwnVehicles (ushort buildingID, ref Building data, TransferManager.TransferReason material, ref int count, ref int cargo, ref int capacity, ref int outside)
	{
		VehicleManager instance = Singleton<VehicleManager>.instance;
		ushort num = data.m_ownVehicles;
		int num2 = 0;
		while (num != 0)
		{
			if ((TransferManager.TransferReason)instance.m_vehicles.m_buffer [(int)num].m_transferType == material)
			{
				VehicleInfo info = instance.m_vehicles.m_buffer [(int)num].Info;
				int a;
				int num3;
				info.m_vehicleAI.GetSize (num, ref instance.m_vehicles.m_buffer [(int)num], out a, out num3);
				cargo += Mathf.Min (a, num3);
				capacity += num3;
				count++;
				if ((instance.m_vehicles.m_buffer [(int)num].m_flags & (Vehicle.Flags.Importing | Vehicle.Flags.Exporting)) != Vehicle.Flags.None)
				{
					outside++;
				}
			}
			num = instance.m_vehicles.m_buffer [(int)num].m_nextOwnVehicle;
			if (++num2 > 16384)
			{
				CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
				break;
			}
		}
	}
}

}