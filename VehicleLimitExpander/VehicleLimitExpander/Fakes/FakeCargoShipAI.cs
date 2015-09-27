using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using System;
using UnityEngine;

namespace Klyte.Unlimiter.Fake
{
	public class FakeCargoShipAI : FakeShipAI
	{
		public int m_cargoCapacity = 1;

		public override void GetBufferStatus (ushort vehicleID, ref Vehicle data, out string localeKey, out int current, out int max)
		{
			localeKey = "Default";
			current = 0;
			max = this.m_cargoCapacity;
			VehicleManager instance = Singleton<VehicleManager>.instance;
			ushort num = data.m_firstCargo;
			int num2 = 0;
			while (num != 0) {
				current++;
				num = instance.m_vehicles.m_buffer [(int)num].m_nextCargo;
				if (++num2 > 65536) {
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			if ((data.m_flags & Vehicle.Flags.DummyTraffic) != Vehicle.Flags.None) {
				Randomizer randomizer = new Randomizer ((int)vehicleID);
				current = randomizer.Int32 (max >> 1, max);
			}
		}
		private bool ArriveAtSource (ushort vehicleID, ref Vehicle data)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			ushort num = data.m_firstCargo;
			data.m_firstCargo = 0;
			int num2 = 0;
			while (num != 0)
			{
				ushort nextCargo = instance.m_vehicles.m_buffer [(int)num].m_nextCargo;
				instance.m_vehicles.m_buffer [(int)num].m_nextCargo = 0;
				instance.m_vehicles.m_buffer [(int)num].m_cargoParent = 0;
				instance.ReleaseVehicle (num);
				num = nextCargo;
				if (++num2 > 65536)
				{
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			data.m_waitCounter = 0;
			data.m_flags |= Vehicle.Flags.WaitingLoading;
			return false;
		}
		
		private bool ArriveAtTarget (ushort vehicleID, ref Vehicle data)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			ushort num = data.m_firstCargo;
			data.m_firstCargo = 0;
			int num2 = 0;
			while (num != 0)
			{
				ushort nextCargo = instance.m_vehicles.m_buffer [(int)num].m_nextCargo;
				instance.m_vehicles.m_buffer [(int)num].m_nextCargo = 0;
				instance.m_vehicles.m_buffer [(int)num].m_cargoParent = 0;
				VehicleInfo info = instance.m_vehicles.m_buffer [(int)num].Info;
				if (data.m_targetBuilding != 0)
				{
					info.m_vehicleAI.SetSource (num, ref instance.m_vehicles.m_buffer [(int)num], data.m_targetBuilding);
					info.m_vehicleAI.SetTarget (num, ref instance.m_vehicles.m_buffer [(int)num], instance.m_vehicles.m_buffer [(int)num].m_targetBuilding);
				}
				num = nextCargo;
				if (++num2 > 65536)
				{
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			data.m_waitCounter = 0;
			data.m_flags |= Vehicle.Flags.WaitingLoading;
			return false;
		}
	}
}
