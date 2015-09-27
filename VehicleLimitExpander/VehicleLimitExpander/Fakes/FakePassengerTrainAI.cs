using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using System;
using UnityEngine;

namespace Klyte.Unlimiter.Fake
{
	public class FakePassengerTrainAI : TrainAI
	{
		public int m_passengerCapacity = 30;

		public override void GetBufferStatus (ushort vehicleID, ref Vehicle data, out string localeKey, out int current, out int max)
		{
			localeKey = "Default";
			current = (int)data.m_transferSize;
			max = this.m_passengerCapacity;
			if (data.m_leadingVehicle == 0) {
				VehicleManager instance = Singleton<VehicleManager>.instance;
				ushort trailingVehicle = data.m_trailingVehicle;
				int num = 0;
				while (trailingVehicle != 0) {
					VehicleInfo info = instance.m_vehicles.m_buffer [(int)trailingVehicle].Info;
					if (instance.m_vehicles.m_buffer [(int)trailingVehicle].m_leadingVehicle != 0) {
						int num2;
						int num3;
						info.m_vehicleAI.GetBufferStatus (trailingVehicle, ref instance.m_vehicles.m_buffer [(int)trailingVehicle], out localeKey, out num2, out num3);
						current += num2;
						max += num3;
					}
					trailingVehicle = instance.m_vehicles.m_buffer [(int)trailingVehicle].m_trailingVehicle;
					if (++num > 65536) {
						CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
						break;
					}
				}
			}
			if ((data.m_flags & Vehicle.Flags.DummyTraffic) != Vehicle.Flags.None) {
				Randomizer randomizer = new Randomizer ((int)vehicleID);
				current = randomizer.Int32 (max >> 1, max);
			}
		}

		public override void SimulationStep (ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
		{
			if (vehicleData.m_leadingVehicle == 0) {
				if ((vehicleData.m_flags & Vehicle.Flags.Stopped) != Vehicle.Flags.None) {
					vehicleData.m_waitCounter += 1;
					if (this.CanLeave (vehicleID, ref vehicleData)) {
						VehicleManager instance = Singleton<VehicleManager>.instance;
						ushort trailingVehicle = vehicleData.m_trailingVehicle;
						bool flag = true;
						int num = 0;
						while (trailingVehicle != 0) {
							VehicleInfo info = instance.m_vehicles.m_buffer [(int)trailingVehicle].Info;
							if (!info.m_vehicleAI.CanLeave (trailingVehicle, ref instance.m_vehicles.m_buffer [(int)trailingVehicle])) {
								flag = false;
								break;
							}
							trailingVehicle = instance.m_vehicles.m_buffer [(int)trailingVehicle].m_trailingVehicle;
							if (++num > 65536) {
								CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
								break;
							}
						}
						if (flag) {
							vehicleData.m_flags &= ~Vehicle.Flags.Stopped;
							vehicleData.m_flags |= Vehicle.Flags.Leaving;
							vehicleData.m_waitCounter = 0;
						}
					}
				} else {
					if ((vehicleData.m_flags & (Vehicle.Flags.GoingBack | Vehicle.Flags.DummyTraffic)) == Vehicle.Flags.None && vehicleData.m_transportLine == 0 && vehicleData.m_targetBuilding != 0 && (Singleton<NetManager>.instance.m_nodes.m_buffer [(int)vehicleData.m_targetBuilding].m_flags & NetNode.Flags.Disabled) != NetNode.Flags.None) {
						this.SetTarget (vehicleID, ref vehicleData, 0);
					}
				}
			}
			base.SimulationStep (vehicleID, ref vehicleData, ref frameData, leaderID, ref leaderData, lodPhysics);
		}

		private void UnloadPassengers (ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
		{
			if (currentStop == 0) {
				return;
			}
			VehicleManager instance = Singleton<VehicleManager>.instance;
			NetManager instance2 = Singleton<NetManager>.instance;
			TransportManager instance3 = Singleton<TransportManager>.instance;
			Vector3 position = instance2.m_nodes.m_buffer [(int)currentStop].m_position;
			Vector3 targetPos = Vector3.zero;
			if (nextStop != 0) {
				targetPos = instance2.m_nodes.m_buffer [(int)nextStop].m_position;
			}
			int num = 0;
			int num2 = 0;
			while (vehicleID != 0) {
				if (data.m_transportLine != 0) {
					BusAI.TransportArriveAtTarget (vehicleID, ref instance.m_vehicles.m_buffer [(int)vehicleID], position, targetPos, ref num, ref instance3.m_lines.m_buffer [(int)data.m_transportLine].m_passengers, nextStop == 0);
				} else {
					BusAI.TransportArriveAtTarget (vehicleID, ref instance.m_vehicles.m_buffer [(int)vehicleID], position, targetPos, ref num, ref instance3.m_passengers [(int)this.m_transportInfo.m_transportType], nextStop == 0);
				}
				vehicleID = instance.m_vehicles.m_buffer [(int)vehicleID].m_trailingVehicle;
				if (++num2 > 65536) {
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			StatisticBase statisticBase = Singleton<StatisticsManager>.instance.Acquire<StatisticArray> (StatisticType.PassengerCount);
			statisticBase.Acquire<StatisticInt32> ((int)this.m_transportInfo.m_transportType, 8).Add (num);
			num += (int)instance2.m_nodes.m_buffer [(int)currentStop].m_tempCounter;
			instance2.m_nodes.m_buffer [(int)currentStop].m_tempCounter = (ushort)Mathf.Min (num, 65535);
		}
	}

}