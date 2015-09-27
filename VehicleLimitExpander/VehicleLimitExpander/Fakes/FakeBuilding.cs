using ColossalFramework;
using ColossalFramework.Math;
using System;
using UnityEngine;

namespace Klyte.Unlimiter.Fake
{
public class FakeBuilding 
{
	public ushort m_guestVehicles;
	public ushort m_ownVehicles;

	public void RemoveGuestVehicle (ushort vehicleID, ref Vehicle data)
	{
		VehicleManager instance = Singleton<VehicleManager>.instance;
		ushort num = 0;
		ushort num2 = this.m_guestVehicles;
		int num3 = 0;
		while (num2 != 0)
		{
			if (num2 == vehicleID)
			{
				if (num != 0)
				{
					instance.m_vehicles.m_buffer [(int)num].m_nextGuestVehicle = data.m_nextGuestVehicle;
				}
				else
				{
					this.m_guestVehicles = data.m_nextGuestVehicle;
				}
				data.m_nextGuestVehicle = 0;
				return;
			}
			num = num2;
			num2 = instance.m_vehicles.m_buffer [(int)num2].m_nextGuestVehicle;
			if (++num3 > 65536)
			{
				CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
				break;
			}
		}
		CODebugBase<LogChannel>.Error (LogChannel.Core, "Vehicle not found!\n" + Environment.StackTrace);
	}
	
	public void RemoveOwnVehicle (ushort vehicleID, ref Vehicle data)
	{
		VehicleManager instance = Singleton<VehicleManager>.instance;
		ushort num = 0;
		ushort num2 = this.m_ownVehicles;
		int num3 = 0;
		while (num2 != 0)
		{
			if (num2 == vehicleID)
			{
				if (num != 0)
				{
					instance.m_vehicles.m_buffer [(int)num].m_nextOwnVehicle = data.m_nextOwnVehicle;
				}
				else
				{
					this.m_ownVehicles = data.m_nextOwnVehicle;
				}
				data.m_nextOwnVehicle = 0;
				return;
			}
			num = num2;
			num2 = instance.m_vehicles.m_buffer [(int)num2].m_nextOwnVehicle;
			if (++num3 > 65536)
			{
				CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
				break;
			}
		}
		CODebugBase<LogChannel>.Error (LogChannel.Core, "Vehicle not found!\n" + Environment.StackTrace);
	}
}

}