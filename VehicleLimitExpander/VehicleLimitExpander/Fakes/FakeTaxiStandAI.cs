using ColossalFramework;
using ColossalFramework.DataBinding;
using ColossalFramework.Math;
using System;
using UnityEngine;

namespace Klyte.Unlimiter.Fake
{
public class FakeTaxiStandAI 
{
	public TransportInfo m_transportInfo;
	public int GetVehicleCount (ushort buildingID, ref Building data)
	{
		int num = 0;
		VehicleManager instance = Singleton<VehicleManager>.instance;
		ushort num2 = data.m_guestVehicles;
		int num3 = 0;
		while (num2 != 0)
		{
			if ((TransferManager.TransferReason)instance.m_vehicles.m_buffer [(int)num2].m_transferType == this.m_transportInfo.m_vehicleReason && (instance.m_vehicles.m_buffer [(int)num2].m_flags & Vehicle.Flags.WaitingCargo) != Vehicle.Flags.None)
			{
				num++;
			}
			num2 = instance.m_vehicles.m_buffer [(int)num2].m_nextGuestVehicle;
			if (++num3 > 65536)
			{
				CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
				break;
			}
		}
		return num;
	}
}
}

